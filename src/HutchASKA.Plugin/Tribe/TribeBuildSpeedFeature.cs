extern alias UnityCore;

using BepInEx.Logging;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using BuildSession = SSSGame.VillagerBuildInteractionConfig.VillagerBuildSession;
using NativeObject = UnityCore::UnityEngine.Object;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeBuildSpeedFeature(ITribeContext tribe) : NativeFeature("tribe.buildspeed", "Build Speed")
{
    private readonly ITribeContext tribeContext = tribe;
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".tribebuildspeed");
    private static readonly ManualLogSource DiagnosticLog = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.TribeBuildSpeed");
    private readonly List<WorkEvent> ownedEvents = new();
    private static TribeBuildSpeedFeature? instance;
    [ThreadStatic] private static WorkEvent? scope;
    private int localEvents, boostedEvents, reports;
    private float reportedMultiplier;
    private string lastDetail = "Waiting for a building work event.";
    public MultiplierSetting Multiplier { get; } = new(1, 5);

    private sealed class WorkEvent(TribeBuildSpeedFeature owner, BuildSession session, BuildInteraction interaction,
        VillagerBuildInteractionConfig original, float multiplier)
    {
        public TribeBuildSpeedFeature Owner { get; } = owner;
        public BuildSession Session { get; } = session;
        public BuildInteraction Interaction { get; } = interaction;
        public VillagerBuildInteractionConfig Original { get; } = original;
        public VillagerBuildInteractionConfig? Replacement { get; private set; }
        private InteractionMoveset? replacementMoveset;
        public bool Active { get; set; } = true;
        public bool Substituted { get; set; }
        public float Before { get; } = interaction.CurrentBuildVolume;
        public float Multiplier { get; } = multiplier;

        public void Prepare()
        {
            var moveset = Original.moveset;
            if (!moveset) throw new InvalidOperationException("Building moveset is unavailable.");
            var baseWork = TribeWorkSpeedMath.ScaleCoefficient(moveset.baseUnarmedDamage, Multiplier);
            var attributeMultiplier = TribeWorkSpeedMath.ScaleCoefficient(moveset.damageMultiplier, Multiplier);
            Replacement = NativeObject.Instantiate(Original).Cast<VillagerBuildInteractionConfig>();
            // Native interaction prompts derive localization keys from the configuration name.
            Replacement.name = Original.name;
            replacementMoveset = NativeObject.Instantiate(moveset).Cast<InteractionMoveset>();
            replacementMoveset.name = moveset.name;
            replacementMoveset.baseUnarmedDamage = baseWork;
            replacementMoveset.damageMultiplier = attributeMultiplier;
            Replacement.moveset = replacementMoveset;
        }

        public void Release()
        {
            if (Active) throw new InvalidOperationException("Building event cleanup is waiting for its native callback to finish.");
            if (Replacement) NativeObject.Destroy(Replacement);
            Replacement = null;
            if (replacementMoveset) NativeObject.Destroy(replacementMoveset);
            replacementMoveset = null;
            Owner.ownedEvents.Remove(this);
        }
    }

    private readonly record struct EventState(WorkEvent? Parent, WorkEvent? Current);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(BuildSession), "_OnAnimatorEvent", new[] { typeof(string) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredProperty(typeof(BuildSession), "BuildConfig")?.GetMethod is { ReturnType: var configType }
        && configType == typeof(VillagerBuildInteractionConfig)
        && typeof(BuildSession).GetProperty("Agent")?.PropertyType == typeof(IInteractionAgent)
        && AccessTools.DeclaredProperty(typeof(BuildSession), "BuildInteraction")?.PropertyType == typeof(BuildInteraction)
        && AccessTools.DeclaredProperty(typeof(BuildSession), "_actionStarted")?.PropertyType == typeof(bool)
        && AccessTools.DeclaredProperty(typeof(BuildSession), "_targetMatched")?.PropertyType == typeof(bool)
        && typeof(VillagerBuildInteractionConfig).GetProperty("moveset")?.CanWrite == true
        && typeof(InteractionMoveset).GetProperty("baseUnarmedDamage")?.CanWrite == true
        && typeof(InteractionMoveset).GetProperty("damageMultiplier")?.CanWrite == true
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Villager building configuration or work coefficient API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        localEvents = boostedEvents = reports = 0;
        lastDetail = "Waiting for a building work event.";
        PublishStatus();
        harmony.Patch(AccessTools.DeclaredMethod(typeof(BuildSession), "_OnAnimatorEvent"),
            prefix: new HarmonyMethod(typeof(TribeBuildSpeedFeature), nameof(EventPrefix)),
            finalizer: new HarmonyMethod(typeof(TribeBuildSpeedFeature), nameof(EventFinalizer)));
        harmony.Patch(AccessTools.DeclaredProperty(typeof(BuildSession), "BuildConfig").GetMethod,
            postfix: new HarmonyMethod(typeof(TribeBuildSpeedFeature), nameof(ConfigPostfix)));
        DiagnosticLog.LogInfo($"Enabled {Multiplier.Value:0}x building; native work submission remains unpatched.");
        return base.TryEnable();
    }

    private string? BuildBlockReason(BuildSession session, BuildInteraction? interaction)
    {
        if (session.SessionState != InteractionSessionState.RUNNING) return "Build session is not running.";
        if (!session._targetMatched) return "Villager has not reached the building position.";
        if (!session._actionStarted) return "Building action has not started.";
        if (interaction == null || !interaction.isActiveAndEnabled) return "Build interaction is inactive.";
        if (session.BuildInteraction == null || session.BuildInteraction.Pointer != interaction.Pointer) return "Build interaction changed.";
        if (interaction._session == null || !interaction._session.isMaster) return "Waiting for local world authority.";
        if (session.Agent == null || !tribeContext.IsCurrentVillager(session.Agent))
            return "Builder is not a current owned villager.";
        return null;
    }

    private void PublishStatus()
    {
        reportedMultiplier = Multiplier.Value;
        StatusReason = $"{reportedMultiplier:0.#}x: {localEvents} tribe work events; {boostedEvents} boosted events. {lastDetail}";
    }

    private static void EventPrefix(BuildSession __instance, string __0, out EventState __state)
    {
        var parent = scope;
        scope = null;
        WorkEvent? current = null;
        var feature = instance;
        if (feature is not null && feature.Multiplier.Value > 1)
            feature.Hosted?.TryExecute(() =>
            {
                if (__instance.Agent == null || !feature.tribeContext.IsCurrentVillager(__instance.Agent)) return;
                var original = __instance.BuildConfig;
                if (!original || !original.moveset || string.IsNullOrEmpty(original.moveset.animatorDamageEvent)
                    || !string.Equals(original.moveset.animatorDamageEvent, __0, StringComparison.Ordinal)) return;
                feature.localEvents = Math.Min(feature.localEvents + 1, 999999);
                var interaction = __instance.BuildInteraction;
                var reason = feature.BuildBlockReason(__instance, interaction);
                if (reason is not null)
                {
                    feature.lastDetail = reason;
                    feature.PublishStatus();
                    return;
                }
                // Read the real getter before publishing the scope. The session cache and shared assets stay native.
                current = new(feature, __instance, interaction, original, feature.Multiplier.Value);
                feature.ownedEvents.Add(current);
                current.Prepare();
                scope = current;
            });
        __state = new(parent, current);
    }

    private static void ConfigPostfix(BuildSession __instance, ref VillagerBuildInteractionConfig __result)
    {
        var current = scope;
        if (current is null || current.Session.Pointer != __instance.Pointer || __result == null
            || __result.Pointer != current.Original.Pointer) return;
        var replace = false;
        current.Owner.Hosted?.TryExecute(() => replace = current.Replacement != null
            && current.Owner.BuildBlockReason(__instance, current.Interaction) is null);
        if (!replace) return;
        __result = current.Replacement!;
        current.Substituted = true;
    }

    private static Exception? EventFinalizer(Exception? __exception, EventState __state)
    {
        scope = __state.Parent;
        var current = __state.Current;
        if (current is null) return __exception;
        current.Active = false;
        try
        {
            current.Owner.Hosted?.TryExecute(() => current.Owner.ObserveResult(current));
        }
        finally
        {
            // The native event has finished with both private objects; cleanup also runs after gating closes.
            try { current.Release(); }
            catch (Exception error)
            {
                DiagnosticLog.LogError($"Building configuration cleanup failed: {error}");
                current.Owner.Hosted?.Disable();
                __exception = __exception is null ? error : new AggregateException(__exception, error);
            }
        }
        return __exception;
    }

    private void ObserveResult(WorkEvent current)
    {
        if (current.Substituted) boostedEvents = Math.Min(boostedEvents + 1, 999999);
        var live = current.Session.Agent != null && tribeContext.IsCurrentVillager(current.Session.Agent) && current.Interaction;
        lastDetail = live ? $"Last work: {current.Before:0.###} -> {current.Interaction.CurrentBuildVolume:0.###}."
            : "The building target changed or completed.";
        PublishStatus();
        if (reports < 12)
        {
            reports++;
            DiagnosticLog.LogInfo($"Event {localEvents}; multiplier={current.Multiplier:0}; substituted={current.Substituted}; {lastDetail}");
        }
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        foreach (var current in ownedEvents.ToArray()) if (!current.Active) current.Release();
        if (ownedEvents.Count != 0) throw new InvalidOperationException("Building configuration cleanup is waiting for an active work event.");
        StatusReason = null;
        base.Disable();
    }

    public override void Tick()
    {
        if (Multiplier.Value != reportedMultiplier) PublishStatus();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}

