extern alias UnityCore;

using HarmonyLib;
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
    public MultiplierSetting Multiplier { get; } = new(1, 4);

    private sealed record GatherOwnership(GatherSession Session, int PlayerIdentity, float Native, float Applied);

    public override CompatibilityResult ProbeCompatibility()
    {
        var valid = typeof(HarvestSession).GetProperty(nameof(HarvestSession._startedAction)) is not null
            && typeof(HarvestSession).GetProperty(nameof(HarvestSession._harvestTime))?.PropertyType == typeof(float)
            && typeof(Animator).GetProperty(nameof(Animator.speed))?.CanWrite == true
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
        instance = this;
        Patch(typeof(HarvestSession), "Update", nameof(HarvestUpdatePrefix), true);
        Patch(typeof(HarvestSession), "Update", nameof(HarvestUpdatePostfix), false);
        Patch(typeof(HarvestSession), "End", nameof(HarvestEndPrefix), true);
        Patch(typeof(HarvestSession), "ResetSession", nameof(HarvestEndPrefix), true);
        Patch(typeof(GatherSession), "_GetGatherVolume", nameof(GatherVolumePostfix), false);
        Patch(typeof(GatherSession), "End", nameof(GatherEndPrefix), true);
        Patch(typeof(GatherSession), "ResetSession", nameof(GatherEndPrefix), true);
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
        if (moveset == null || moveset.allowMeleeAttackWhileHarvesting
            || !string.IsNullOrEmpty(moveset.animatorDamageEvent)) return;
        // Native Update uses this timer only when no damage animation event is configured.
        session._harvestTime += UnityCore::UnityEngine.Time.deltaTime * (Multiplier.Value - 1);
    }

    private void UpdateHarvest(HarvestSession session)
    {
        if (!IsLocal(session.PlayerAgent, out var player))
        {
            if (harvest?.Pointer == session.Pointer) RestoreHarvest();
            return;
        }
        var moveset = session.HarvestInteraction?.GetAttackMoveset();
        if (Multiplier.Value == 1 || session.SessionState != InteractionSessionState.RUNNING
            || session._harvestTime < 0 || string.IsNullOrEmpty(session._startedAction)
            || moveset == null || moveset.allowMeleeAttackWhileHarvesting)
        {
            if (harvest?.Pointer == session.Pointer) RestoreHarvest();
            return;
        }
        var animator = session._playerGeo?.Animator;
        if (animator == null || player!.Geometry?.Animator != animator)
        {
            if (harvest?.Pointer == session.Pointer) RestoreHarvest();
            return;
        }
        if (harvest is not null && (harvest.Pointer != session.Pointer
            || animation.Owner != animator.GetInstanceID() || playerIdentity != player.GetInstanceID()))
            RestoreHarvest();
        harvest = session;
        playerIdentity = player.GetInstanceID();
        var target = animation.Target(animator.GetInstanceID(), animator.speed, Multiplier.Value);
        animator.speed = target;
        animation.RecordApplied(target);
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
        base.Disable();
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
        feature?.Hosted?.TryExecute(() => feature.UpdateHarvest(__instance));
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
