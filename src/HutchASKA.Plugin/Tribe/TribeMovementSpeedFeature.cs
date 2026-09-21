using BepInEx.Logging;
using System.Diagnostics;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using SSSGame.AI;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeMovementSpeedFeature(ITribeContext tribe)
    : NativeFeature("tribe.movement", "Tribe Movement Speed")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".tribe.movement");
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.TribeMovement");
    private static TribeMovementSpeedFeature? active;
    [ThreadStatic] private static UpdateScope scope;
    private readonly record struct UpdateScope(TribeMovementSpeedFeature? Feature, IntPtr Controller);
    private int samples;
    private long nextStatusAt;
    private (float Multiplier, int Samples)? published;
    public MultiplierSetting Multiplier { get; } = new(1, 5);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(CreatureController), "Update", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(CreatureController), "_GetMovementSpeed", new[] { typeof(AIMovementSpeed) })?.ReturnType == typeof(float)
        && typeof(CreatureController).GetProperty("NavAgentControllable")?.PropertyType == typeof(INavAgentControllable)
        && AccessTools.DeclaredMethod(typeof(Villager), "GetControlAI", Type.EmptyTypes)?.ReturnType == typeof(IControlAI)
        && typeof(CreatureController).GetProperty("_linkTraversalState")?.PropertyType == typeof(LinkTraversalState)
        && typeof(LinkTraversalState).GetProperty("valid")?.PropertyType == typeof(bool)
        && new[] { "_initialized", "agentReady", "IsOwner", "IsGrounded", "IsSwimming", "IsOnLadder",
            "InVehicle", "CanMove", "IsStopped", "_performingRootMotion", "_lockMovement" }
            .All(name => typeof(CreatureController).GetProperty(name)?.PropertyType == typeof(bool))
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("The owned villager ground-navigation speed or traversal-state API is unavailable.");

    public override bool TryEnable()
    {
        active = this;
        samples = 0;
        nextStatusAt = 0;
        published = null;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CreatureController), "Update", Type.EmptyTypes),
            prefix: new HarmonyMethod(typeof(TribeMovementSpeedFeature), nameof(UpdatePrefix)),
            finalizer: new HarmonyMethod(typeof(TribeMovementSpeedFeature), nameof(UpdateFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CreatureController), "_GetMovementSpeed", new[] { typeof(AIMovementSpeed) }),
            postfix: new HarmonyMethod(typeof(TribeMovementSpeedFeature), nameof(SpeedPostfix)));
        PublishStatus();
        return base.TryEnable();
    }

    private static void UpdatePrefix(CreatureController __instance, out UpdateScope __state)
    {
        __state = scope;
        scope = active is { } feature && __instance != null ? new(feature, __instance.Pointer) : default;
    }

    private static Exception? UpdateFinalizer(Exception? __exception, UpdateScope __state)
    {
        scope = __state;
        return __exception;
    }

    private bool IsOrdinaryOwnedMovement(CreatureController controller)
    {
        if (!controller || !controller._initialized || !controller.agentReady || !controller.IsOwner
            || !controller.IsGrounded || controller.IsSwimming || controller.IsOnLadder || controller.InVehicle
            || !controller.CanMove || controller.IsStopped || controller._performingRootMotion || controller._lockMovement
            || controller._linkTraversalState.valid) return false;
        var agent = controller._agent;
        if (!agent || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isOnOffMeshLink) return false;
        var villager = controller.NavAgentControllable?.TryCast<Villager>();
        return villager != null && villager.GetControlAI()?.Pointer == controller.Pointer
            && tribe.IsCurrentVillager(villager);
    }

    private static void SpeedPostfix(CreatureController __instance, ref float __result)
    {
        var feature = active;
        if (feature is null || scope.Feature != feature || __instance == null
            || scope.Controller != __instance.Pointer || feature.Multiplier.Value == 1) return;
        var original = __result;
        var result = original;
        if (feature.Hosted?.TryExecute(() =>
        {
            // Link setup marks valid before its own speed query; traversal queries remain native.
            if (!feature.IsOrdinaryOwnedMovement(__instance)) return;
            result = MovementDelta.ScaleNativeSpeed(original, feature.Multiplier.Value);
            if (result == original) return;
            feature.samples = Math.Min(feature.samples + 1, 999999);
            if (feature.samples <= 6)
                Log.LogInfo($"Owned villager ground speed: {original:0.###} -> {result:0.###} ({feature.Multiplier.Value:0}x); native acceleration and navigation retained.");
        }) == true) __result = result;
    }

    private void PublishStatus()
    {
        var current = (Multiplier.Value, samples);
        if (published == current) return;
        var now = Stopwatch.GetTimestamp();
        if (published is { } previous && previous.Multiplier == Multiplier.Value && now < nextStatusAt) return;
        published = current;
        nextStatusAt = now + Stopwatch.Frequency;
        StatusReason = $"{Multiplier.Value:0}x owned villagers' ground-navigation target speed; {samples} boosted samples. Swimming, vehicles, ladders and link traversal stay normal.";
    }

    public override void Tick() => PublishStatus();

    public override void Disable()
    {
        active = null;
        scope = default;
        harmony.UnpatchSelf();
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
