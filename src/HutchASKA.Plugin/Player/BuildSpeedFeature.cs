using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Items;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using BuildSession = SSSGame.PlayerBuildInteractionConfig.PlayerBuildInteractionSession;

namespace HutchASKA.Plugin.Player;

internal sealed class BuildSpeedFeature(IPlayerContext players) : NativeFeature("player.buildspeed", "Build Speed")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".buildspeed");
    private readonly IPlayerContext playerContext = players;
    private static BuildSpeedFeature? instance;
    [ThreadStatic] private static BuildScope scope;
    private readonly record struct BuildScope(BuildSession? Session, IntPtr Interaction);
    private int localEvents;
    private int scaledRequests;
    private float reportedMultiplier;
    private string lastDetail = "Waiting for a building animation event.";
    public MultiplierSetting Multiplier { get; } = new(1, 4);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(BuildSession), "_OnAnimatorEvent", new[] { typeof(string) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(BuildInteraction), "RequestAddBuildVolume", new[] { typeof(float).MakeByRefType() })?.ReturnType == typeof(void)
        && typeof(BuildSession).GetProperty("Agent")?.PropertyType == typeof(PlayerInteractionAgent)
        && typeof(BuildSession).GetProperty("BuildInteraction")?.PropertyType == typeof(BuildInteraction)
        && typeof(BuildSession).GetProperty("_actionStarted")?.PropertyType == typeof(bool)
        && typeof(BuildSession).GetProperty("_targetMatched")?.PropertyType == typeof(bool)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Local player construction contribution API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        localEvents = scaledRequests = 0;
        lastDetail = "Waiting for a building animation event.";
        PublishStatus();
        harmony.Patch(AccessTools.DeclaredMethod(typeof(BuildSession), "_OnAnimatorEvent"),
            prefix: new HarmonyMethod(typeof(BuildSpeedFeature), nameof(EventPrefix)),
            finalizer: new HarmonyMethod(typeof(BuildSpeedFeature), nameof(EventFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(BuildInteraction), "RequestAddBuildVolume"),
            prefix: new HarmonyMethod(typeof(BuildSpeedFeature), nameof(WorkPrefix)),
            postfix: new HarmonyMethod(typeof(BuildSpeedFeature), nameof(WorkPostfix)),
            finalizer: new HarmonyMethod(typeof(BuildSpeedFeature), nameof(WorkFinalizer)));
        return base.TryEnable();
    }

    private string? LocalBuildBlockReason(BuildSession? session, BuildInteraction? interaction)
    {
        if (session is null || session.SessionState != InteractionSessionState.RUNNING) return "Build session is not running.";
        if (!session._targetMatched) return "Player has not reached the building position.";
        if (!session._actionStarted) return "Building action has not started.";
        if (interaction == null || !interaction.isActiveAndEnabled) return "Build interaction is inactive.";
        if (session.BuildInteraction == null || session.BuildInteraction.Pointer != interaction.Pointer)
            return "Build interaction changed.";
        if (interaction._session == null || !interaction._session.isMaster) return "Waiting for local world authority.";
        if (!playerContext.TryGetLocalPlayer(out var player) || session.Agent == null || session.Agent.GetCharacter() != player)
            return "Builder is not the current local player.";
        return null;
    }

    private void PublishStatus()
    {
        reportedMultiplier = Multiplier.Value;
        StatusReason = $"{reportedMultiplier:0.#}x: {localEvents} local animation events; {scaledRequests} scaled work requests. "
            + (reportedMultiplier == 1 ? "Normal building speed." : lastDetail);
    }

    private void RecordEvent(string detail)
    {
        localEvents = Math.Min(localEvents + 1, 999999);
        var changed = detail != lastDetail;
        lastDetail = detail;
        // Bounded counters and sampled UI text provide evidence without per-event logging.
        if (changed || localEvents <= 8 || localEvents % 32 == 0) PublishStatus();
    }

    private static void EventPrefix(BuildSession __instance, out BuildScope __state)
    {
        __state = scope;
        scope = default;
        var feature = instance;
        if (feature is null || feature.Multiplier.Value == 1) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (!feature.playerContext.TryGetLocalPlayer(out var player) || __instance.Agent == null
                || __instance.Agent.GetCharacter() != player) return;
            var interaction = __instance.BuildInteraction;
            var reason = feature.LocalBuildBlockReason(__instance, interaction);
            feature.RecordEvent(reason ?? (feature.scaledRequests > 0
                ? "Native building work is being scaled."
                : "Builder ready; waiting for the native work request."));
            if (reason is null) scope = new(__instance, interaction.Pointer);
        });
    }

    private static Exception? EventFinalizer(Exception? __exception, BuildScope __state)
    {
        scope = __state;
        return __exception;
    }

    private static void WorkPrefix(BuildInteraction __instance, ref float __0, out TemporaryFloatOverride __state)
    {
        __state = default;
        var feature = instance;
        if (feature is null || __instance == null || scope.Interaction != __instance.Pointer) return;
        var current = scope;
        scope = default;
        var native = __0;
        var scaled = native;
        if (feature.Hosted?.TryExecute(() =>
        {
            var reason = feature.LocalBuildBlockReason(current.Session, __instance);
            if (reason is null)
                scaled = BuildWorkAmount.Scale(native, feature.Multiplier.Value);
            else
            {
                feature.lastDetail = reason;
                feature.PublishStatus();
            }
        }) != true || scaled == native) return;
        // Keep native completion/hit processing; restore the caller's local amount
        // before its stamina, injury, proficiency and cancellation logic continues.
        __state = new TemporaryFloatOverride(true, native);
        __0 = scaled;
        feature.scaledRequests = Math.Min(feature.scaledRequests + 1, 999999);
        feature.lastDetail = "Native building work is being scaled.";
        if (feature.scaledRequests <= 8 || feature.scaledRequests % 32 == 0) feature.PublishStatus();
    }

    private static void WorkPostfix(ref float __0, TemporaryFloatOverride __state) => __state.Restore(ref __0);

    private static Exception? WorkFinalizer(Exception? __exception, ref float __0, TemporaryFloatOverride __state)
    {
        __state.Restore(ref __0);
        return __exception;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        scope = default;
        StatusReason = null;
        base.Disable();
    }

    public override void Tick()
    {
        if (Multiplier.Value != reportedMultiplier) PublishStatus();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
