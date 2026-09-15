using System.Reflection;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame.Controllers;
using MotionVector = System.Numerics.Vector3;

namespace HutchASKA.Plugin.Player;

internal sealed class MovementSpeedFeature(IPlayerContext players) : NativeFeature("player.movement", "Movement Speed")
{
    public MultiplierSetting Multiplier { get; } = new(1, 5);
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".movement");
    private static MovementSpeedFeature? active;
    private static readonly string[] RequiredFlags =
    {
        "isGrounded", "isRaven", "IsSwimming", "IsClimbing", "IsSliding", "isCarting", "isRowing", "hasExternalControl"
    };
    private static MethodInfo? AnimatorMove() => AccessTools.DeclaredMethod(typeof(CharacterMovement), "OnAnimatorMove", Type.EmptyTypes);

    public override CompatibilityResult ProbeCompatibility() =>
        AnimatorMove() is { IsStatic: false, ReturnType: var returnType } && returnType == typeof(void)
        && typeof(CharacterMovement).GetProperty("_totalDeltaRootMotion") is { CanRead: true, CanWrite: true } motion
        && motion.PropertyType == typeof(Vector3)
        && typeof(CharacterMovement).GetProperty("lastRawMovement") is { CanRead: true } input && input.PropertyType == typeof(Vector2)
        && RequiredFlags.All(name => typeof(CharacterMovement).GetProperty(name) is { CanRead: true } flag && flag.PropertyType == typeof(bool))
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native movement root-motion API is unavailable.");

    public override bool TryEnable()
    {
        active = this;
        harmony.Patch(AnimatorMove(),
            prefix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(BeforeAnimatorMove)),
            postfix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(AfterAnimatorMove)));
        return base.TryEnable();
    }

    private bool CanScale(CharacterMovement movement) =>
        players.TryGetLocalPlayer(out var player) && player!.GetCharacterMovement() == movement
        && movement.isGrounded && !movement.isRaven && !movement.IsSwimming && !movement.IsClimbing
        && !movement.IsSliding && !movement.isCarting && !movement.isRowing && !movement.hasExternalControl
        && movement.lastRawMovement.sqrMagnitude > 0;

    internal readonly record struct MotionSample(MotionVector Before, float Multiplier);

    public static void BeforeAnimatorMove(CharacterMovement __instance, out MotionSample __state)
    {
        var sample = default(MotionSample);
        var feature = active;
        if (feature is not null && feature.Multiplier.Value > 1)
            feature.Hosted?.TryExecute(() =>
            {
                if (!feature.CanScale(__instance)) return;
                var before = __instance._totalDeltaRootMotion;
                sample = new MotionSample(new MotionVector(before.x, before.y, before.z), feature.Multiplier.Value);
            });
        __state = sample;
    }

    public static void AfterAnimatorMove(CharacterMovement __instance, MotionSample __state)
    {
        if (__state.Multiplier <= 1) return;
        var feature = active;
        feature?.Hosted?.TryExecute(() =>
        {
            if (feature.Multiplier.Value <= 1 || !feature.CanScale(__instance)) return;
            var after = __instance._totalDeltaRootMotion;
            // Scale this callback's increment only; ASKA can accumulate several samples before consumption.
            var scaled = MovementDelta.ScaleHorizontal(__state.Before,
                new MotionVector(after.x, after.y, after.z), __state.Multiplier);
            __instance._totalDeltaRootMotion = new Vector3(scaled.X, scaled.Y, scaled.Z);
        });
    }

    public override void Disable()
    {
        active = null;
        harmony.UnpatchSelf();
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
