extern alias UnityCore;

using HarmonyLib;
using System.Diagnostics;
using BepInEx.Logging;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using UnityEngine;
using GatherSession = SSSGame.PlayerGatherInteractionConfig.PlayerGatherInteractionSession;
using HarvestSession = SSSGame.PlayerHarvestInteractionConfig.PlayerHarvestInteractionSession;

namespace HutchASKA.Plugin.Player;

internal sealed class HarvestSpeedFeature(IPlayerContext players) : NativeFeature("player.harvest", "Harvest Speed")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".harvest");
    private readonly HarvestSpeedController animation = new();
    private HarvestSession? harvest;
    private int playerIdentity;
    private readonly Dictionary<IntPtr, GatherOwnership> gathering = new();
    private static HarvestSpeedFeature? instance;
    private static readonly ManualLogSource DiagnosticLog = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Harvest");
    private readonly long[] blocked = new long[8];
    private int reports;
    private long nextReport, updateCalls, localUpdates, animationWrites, timerAdvances;
    private string lastMoveset = "", lastSessionAction = "", lastGeometryAction = "";
    private bool lastMeleeAllowed;
    private int lastExpectedAction, lastAnimatorAction;
    private float lastObservedSpeed, lastAppliedSpeed;
    private bool DiagnosticsActive => reports < 12;
    public MultiplierSetting Multiplier { get; } = new(1, 4);

    private sealed record GatherOwnership(GatherSession Session, int PlayerIdentity, float Native, float Applied);

    public override CompatibilityResult ProbeCompatibility()
    {
        var valid = typeof(HarvestSession).GetProperty(nameof(HarvestSession._startedAction)) is not null
            && typeof(HarvestSession).GetProperty(nameof(HarvestSession._harvestTime))?.PropertyType == typeof(float)
            && typeof(Animator).GetProperty(nameof(Animator.speed))?.CanWrite == true
            && typeof(Animator).GetMethod("GetInteger", new[] { typeof(string) })?.ReturnType == typeof(int)
            && typeof(CharacterGeometry).GetProperty("_lastStartedAction")?.PropertyType == typeof(string)
            && AccessTools.DeclaredMethod(typeof(PlayerInteractionAgent), "SetCombatMode", new[] { typeof(bool) })?.ReturnType == typeof(void)
            && AccessTools.DeclaredMethod(typeof(HarvestSession), "Update", Type.EmptyTypes) is not null
            && AccessTools.DeclaredMethod(typeof(HarvestSession), "End", new[] { typeof(InteractionSessionState) }) is not null
            && AccessTools.DeclaredMethod(typeof(HarvestSession), "ResetSession", Type.EmptyTypes) is not null
            && AccessTools.DeclaredMethod(typeof(GatherSession), "_GetGatherVolume", Type.EmptyTypes)?.ReturnType == typeof(float)
            && AccessTools.DeclaredMethod(typeof(GatherSession), "End", new[] { typeof(InteractionSessionState) }) is not null
            && AccessTools.DeclaredMethod(typeof(GatherSession), "ResetSession", Type.EmptyTypes) is not null;
        return valid ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("Local harvest animation or gathering work API is unavailable.");
    }

    public override bool TryEnable()
    {
        reports = 0;
        ResetDiagnostics();
        lastMoveset = lastSessionAction = lastGeometryAction = "";
        lastMeleeAllowed = false;
        lastExpectedAction = lastAnimatorAction = 0;
        lastObservedSpeed = lastAppliedSpeed = 0;
        nextReport = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5;
        StatusReason = "Harvest diagnostics active for 60 seconds; chop or mine to sample the boost.";
        instance = this;
        Patch(typeof(HarvestSession), "Update", nameof(HarvestUpdatePrefix), true);
        Patch(typeof(HarvestSession), "Update", nameof(HarvestUpdatePostfix), false);
        Patch(typeof(HarvestSession), "End", nameof(HarvestEndPrefix), true);
        Patch(typeof(HarvestSession), "ResetSession", nameof(HarvestEndPrefix), true);
        Patch(typeof(GatherSession), "_GetGatherVolume", nameof(GatherVolumePostfix), false);
        Patch(typeof(GatherSession), "End", nameof(GatherEndPrefix), true);
        Patch(typeof(GatherSession), "ResetSession", nameof(GatherEndPrefix), true);
        Patch(typeof(PlayerInteractionAgent), "SetCombatMode", nameof(CombatModePrefix), true);
        DiagnosticLog.LogInfo($"Enabled harvest {Multiplier.Value:0}x; observing action ownership for 60 seconds.");
        return base.TryEnable();
    }

    private void Patch(Type type, string method, string callback, bool prefix)
    {
        var patch = new HarmonyMethod(typeof(HarvestSpeedFeature), callback);
        harmony.Patch(AccessTools.DeclaredMethod(type, method), prefix: prefix ? patch : null,
            postfix: prefix ? null : patch);
    }

    private bool IsLocal(PlayerInteractionAgent? agent, out PlayerCharacter? player) =>
        players.TryGetLocalPlayer(out player) && agent != null && agent.GetCharacter() == player;

    private void AdvanceFallbackTimer(HarvestSession session)
    {
        if (Multiplier.Value == 1 || session.SessionState != InteractionSessionState.RUNNING
            || session._harvestTime < 0 || !IsLocal(session.PlayerAgent, out _)) return;
        var moveset = session.HarvestInteraction?.GetAttackMoveset();
        if (moveset == null || !string.IsNullOrEmpty(moveset.animatorDamageEvent)
            || !TryGetActiveHarvestAnimator(session, out _, out _, false)) return;
        // Native Update uses this timer only when no damage animation event is configured.
        session._harvestTime += UnityCore::UnityEngine.Time.deltaTime * (Multiplier.Value - 1);
        if (DiagnosticsActive) timerAdvances++;
    }

    private bool TryGetActiveHarvestAnimator(HarvestSession session, out PlayerCharacter? player, out Animator? animator, bool record)
    {
        animator = null;
        if (!IsLocal(session.PlayerAgent, out player)) return Block(0, record);
        if (record && DiagnosticsActive) localUpdates++;
        if (session.SessionState != InteractionSessionState.RUNNING) return Block(1, record);
        if (session._harvestTime < 0) return Block(2, record);
        var moveset = session.HarvestInteraction?.GetAttackMoveset();
        if (moveset == null) return Block(3, record);
        var geometry = session._playerGeo;
        if (DiagnosticsActive)
        {
            lastMoveset = moveset.name;
            lastMeleeAllowed = moveset.allowMeleeAttackWhileHarvesting;
            lastSessionAction = session._startedAction ?? "";
            lastGeometryAction = geometry?._lastStartedAction ?? "";
            lastExpectedAction = moveset.actionId;
        }
        if (string.IsNullOrEmpty(session._startedAction) || string.IsNullOrEmpty(moveset.attackActionName)) return Block(4, record);
        if (session.PlayerAgent.IsInCombatMode) return Block(5, record);
        animator = geometry?.Animator;
        if (animator == null || player!.Geometry?.Animator != animator) return Block(6, record);
        var animatorAction = animator.GetInteger(moveset.attackActionName);
        if (DiagnosticsActive) lastAnimatorAction = animatorAction;
        // This flag permits melee alongside harvest; it is true on normal axe/pickaxe assets.
        // Native RunAction/StopAction maintain these action names and animator integers.
        if (!HarvestSpeedController.HasActiveHarvestAction(session._startedAction, moveset.attackActionName,
                geometry!._lastStartedAction, moveset.actionId, animatorAction)) return Block(7, record);
        return true;
    }

    private bool Block(int reason, bool record)
    {
        if (record && DiagnosticsActive) blocked[reason]++;
        return false;
    }

    private void UpdateHarvest(HarvestSession session, bool nativeUpdate = false)
    {
        if (nativeUpdate && DiagnosticsActive) updateCalls++;
        if (Multiplier.Value == 1 || !TryGetActiveHarvestAnimator(session, out var player, out var animator, nativeUpdate))
        {
            if (harvest?.Pointer == session.Pointer) RestoreHarvest();
            return;
        }
        if (harvest is not null && (harvest.Pointer != session.Pointer
            || animation.Owner != animator!.GetInstanceID() || playerIdentity != player!.GetInstanceID()))
            RestoreHarvest();
        harvest = session;
        playerIdentity = player!.GetInstanceID();
        var observed = animator!.speed;
        var target = animation.Target(animator.GetInstanceID(), observed, Multiplier.Value);
        animator.speed = target;
        animation.RecordApplied(target);
        if (DiagnosticsActive)
        {
            animationWrites++;
            lastObservedSpeed = observed;
            lastAppliedSpeed = target;
        }
    }

    private void RestoreHarvest()
    {
        if (harvest is null) return;
        // Resolve the live player's animator instead of dereferencing an old scene wrapper.
        if (players.TryGetLocalPlayer(out var player) && player!.GetInstanceID() == playerIdentity)
        {
            var animator = player.Geometry?.Animator;
            if (animator != null && animation.RestoreTarget(animator.GetInstanceID(), animator.speed) is { } native)
                animator.speed = native;
        }
        harvest = null;
        animation.Clear();
    }

    private float GatherVolume(GatherSession session, float native)
    {
        if (Multiplier.Value == 1 || session.SessionState != InteractionSessionState.RUNNING
            || !IsLocal(session.PlayerAgent, out var player)) return native;
        var target = HarvestSpeedController.GatherThreshold(native, Multiplier.Value);
        gathering[session.Pointer] = new(session, player!.GetInstanceID(), native, target);
        return target;
    }

    private void RestoreGather(IntPtr identity)
    {
        if (!gathering.TryGetValue(identity, out var owned)) return;
        if (players.TryGetLocalPlayer(out var player) && player!.GetInstanceID() == owned.PlayerIdentity
            && owned.Session.PlayerAgent != null && owned.Session.PlayerAgent.GetCharacter() == player)
        {
            var session = owned.Session;
            if (session._volumeTarget == owned.Applied)
            {
                session._volumeProgress = HarvestSpeedController.RestoreGatherProgress(
                    session._volumeProgress, owned.Applied, owned.Native);
                session._volumeTarget = owned.Native;
            }
        }
        gathering.Remove(identity);
    }

    public override void Tick()
    {
        ReportDiagnostics();
        var currentPlayer = players.TryGetLocalPlayer(out var local) ? local!.GetInstanceID() : (int?)null;
        foreach (var identity in gathering.Where(entry => entry.Value.PlayerIdentity != currentPlayer)
                     .Select(entry => entry.Key).ToArray())
            gathering.Remove(identity);
        if (Multiplier.Value == 1)
        {
            RestoreHarvest();
            foreach (var identity in gathering.Keys.ToArray()) RestoreGather(identity);
            return;
        }
        if (harvest is not null)
        {
            if (!players.TryGetLocalPlayer(out var player) || player!.GetInstanceID() != playerIdentity)
                RestoreHarvest();
            else UpdateHarvest(harvest);
        }
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        RestoreHarvest();
        foreach (var identity in gathering.Keys.ToArray()) RestoreGather(identity);
        StatusReason = null;
        base.Disable();
    }

    private void ReportDiagnostics()
    {
        if (!DiagnosticsActive || Stopwatch.GetTimestamp() < nextReport) return;
        reports++;
        StatusReason = $"Harvest sample: {animationWrites} animation boosts; {localUpdates}/{updateCalls} local updates; last action {lastSessionAction}, speed {lastAppliedSpeed:0.00}.";
        DiagnosticLog.LogInfo($"Sample {reports}/12; multiplier={Multiplier.Value:0}; updates all/local={updateCalls}/{localUpdates}; "
            + $"animationWrites={animationWrites}; timerAdvances={timerAdvances}; moveset={lastMoveset}; meleeAllowed={lastMeleeAllowed}; "
            + $"session/geometryAction={lastSessionAction}/{lastGeometryAction}; action expected/observed={lastExpectedAction}/{lastAnimatorAction}; "
            + $"speed observed/applied={lastObservedSpeed:0.000}/{lastAppliedSpeed:0.000}; "
            + $"blocked nonlocal/inactive/matching/noMoveset/noAction/combat/wrongAnimator/actionMismatch={string.Join("/", blocked)}");
        ResetDiagnostics();
        nextReport = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5;
    }

    private void ResetDiagnostics()
    {
        updateCalls = localUpdates = animationWrites = timerAdvances = 0;
        Array.Clear(blocked, 0, blocked.Length);
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }

    private static void HarvestUpdatePrefix(HarvestSession __instance)
    {
        var feature = instance;
        feature?.Hosted?.TryExecute(() => feature.AdvanceFallbackTimer(__instance));
    }

    private static void HarvestUpdatePostfix(HarvestSession __instance)
    {
        var feature = instance;
        feature?.Hosted?.TryExecute(() => feature.UpdateHarvest(__instance, true));
    }

    private static void CombatModePrefix(PlayerInteractionAgent __instance, bool __0)
    {
        var feature = instance;
        if (!__0 || feature?.harvest is null) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (feature.IsLocal(__instance, out _)) feature.RestoreHarvest();
        });
    }

    private static void HarvestEndPrefix(HarvestSession __instance)
    {
        var feature = instance;
        if (feature?.harvest?.Pointer == __instance.Pointer)
            feature.Hosted?.TryExecute(feature.RestoreHarvest);
    }

    private static void GatherVolumePostfix(GatherSession __instance, ref float __result)
    {
        var feature = instance;
        if (feature is null) return;
        var result = __result;
        if (feature.Hosted?.TryExecute(() => result = feature.GatherVolume(__instance, result)) == true)
            __result = result;
    }

    private static void GatherEndPrefix(GatherSession __instance)
    {
        var feature = instance;
        feature?.Hosted?.TryExecute(() => feature.RestoreGather(__instance.Pointer));
    }
}
