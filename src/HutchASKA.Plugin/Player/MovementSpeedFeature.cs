extern alias UnityCore;

using System.Reflection;
using System.Diagnostics;
using BepInEx.Logging;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame.Controllers;
using MotionVector = System.Numerics.Vector3;
using NativeObject = UnityCore::UnityEngine.Object;

namespace HutchASKA.Plugin.Player;

internal sealed class MovementSpeedFeature(IPlayerContext players) : NativeFeature("player.movement", "Movement Speed")
{
    public MultiplierSetting Multiplier { get; } = new(1, 5);
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".movement");
    private static MovementSpeedFeature? active;
    private static readonly ManualLogSource DiagnosticLog = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Movement");
    private MovementStats? sourceStats;
    private MovementStats? privateStats;
    private CharacterMovement? commandOwner;
    private long nextReport;
    private int reports;
    private long animatorCalls, localAnimatorCalls, eligibleAnimatorCalls, scaledSamples, commandCalls, localCommandCalls, boostedCommands, applyCalls;
    private double nativeRootDistance, addedRootDistance, consumedRootDistance;
    private float lastRootRatio, lastNativeSpeed, lastAppliedSpeed, lastDesiredSpeed;
    private readonly long[] blockedStates = new long[9];
    private bool DiagnosticsActive => reports < 12;
    private static readonly string[] RequiredFlags =
    {
        "isGrounded", "isRaven", "IsSwimming", "IsClimbing", "IsSliding", "isCarting", "isRowing", "hasExternalControl"
    };
    private static MethodInfo? AnimatorMove() => AccessTools.DeclaredMethod(typeof(CharacterMovement), "OnAnimatorMove", Type.EmptyTypes);
    private static MethodInfo? MoveCommand() => AccessTools.DeclaredMethod(typeof(CharacterMovement), "MoveCommand", new[] { typeof(Vector2), typeof(Vector3), typeof(Vector3) });

    public override CompatibilityResult ProbeCompatibility() =>
        AnimatorMove() is { IsStatic: false, ReturnType: var returnType } && returnType == typeof(void)
        && MoveCommand() is { IsStatic: false, ReturnType: var commandResult } && commandResult == typeof(void)
        && AccessTools.DeclaredMethod(typeof(CharacterMovement), "_ApplyRootMotion", Type.EmptyTypes)?.ReturnType == typeof(void)
        && typeof(CharacterMovement).GetProperty("parameters") is { CanRead: true, CanWrite: true, PropertyType: var statsType } && statsType == typeof(MovementStats)
        && typeof(CharacterMovement).GetProperty("animationRootMotionRatio") is { CanRead: true, PropertyType: var ratioType } && ratioType == typeof(float)
        && typeof(CharacterMovement).GetProperty("_finalDirection") is { CanRead: true, PropertyType: var directionType } && directionType == typeof(Vector3)
        && typeof(CharacterMovement).GetProperty("_totalDeltaRootMotion") is { CanRead: true, CanWrite: true } motion
        && motion.PropertyType == typeof(Vector3)
        && typeof(CharacterMovement).GetProperty("lastRawMovement") is { CanRead: true } input && input.PropertyType == typeof(Vector2)
        && RequiredFlags.All(name => typeof(CharacterMovement).GetProperty(name) is { CanRead: true } flag && flag.PropertyType == typeof(bool))
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native movement command or root-motion API is unavailable.");

    public override bool TryEnable()
    {
        reports = 0;
        ResetCounters();
        nextReport = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5;
        StatusReason = "Movement diagnostics active for 60 seconds; walk on level ground to sample the boost.";
        active = this;
        harmony.Patch(AnimatorMove(),
            prefix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(BeforeAnimatorMove)),
            postfix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(AfterAnimatorMove)));
        harmony.Patch(MoveCommand(),
            prefix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(BeforeMoveCommand)),
            postfix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(AfterMoveCommand)),
            finalizer: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(FinishMoveCommand)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CharacterMovement), "_ApplyRootMotion"),
            prefix: new HarmonyMethod(typeof(MovementSpeedFeature), nameof(BeforeApplyRootMotion)));
        DiagnosticLog.LogInfo($"Enabled movement {Multiplier.Value:0.0}x; observing native command and root-motion paths for 60 seconds.");
        return base.TryEnable();
    }

    private bool IsLocal(CharacterMovement movement) =>
        movement && players.TryGetLocalPlayer(out var player) && player!.GetCharacterMovement() == movement;

    private bool CanScale(CharacterMovement movement, Vector2 input, bool recordBlock = false)
    {
        // Native PlayerDrive requires this permission; target matching clears it to suppress player input.
        var blocked = MovementEligibility.GetBlockReason(movement.isGrounded, movement.isRaven, movement.IsSwimming,
            movement.IsClimbing, movement.IsSliding, movement.isCarting, movement.isRowing,
            movement.hasExternalControl, input.sqrMagnitude);
        if (blocked == MovementBlockReason.None) return true;
        if (recordBlock && DiagnosticsActive) blockedStates[(int)blocked]++;
        return false;
    }

    internal readonly record struct CommandSample(MovementSpeedFeature? Feature, MovementStats? Original, MovementStats? Replacement);

    public static void BeforeMoveCommand(CharacterMovement __instance, Vector2 __0, out CommandSample __state)
    {
        var sample = default(CommandSample);
        var feature = active;
        if (feature is not null)
        {
            if (feature.DiagnosticsActive) feature.commandCalls++;
            feature.Hosted?.TryExecute(() =>
            {
                if (!feature.IsLocal(__instance)) return;
                if (feature.DiagnosticsActive) feature.localCommandCalls++;
                if (feature.Multiplier.Value <= 1 || !feature.CanScale(__instance, __0, true)) return;
                // A nested command shares the already prepared speed; never multiply the clone again.
                if (feature.commandOwner is not null) return;
                var source = __instance.parameters;
                if (!source) throw new InvalidOperationException("Local movement parameters are unavailable.");
                feature.PrepareStats(source);
                var replacement = feature.privateStats!;
                sample = new CommandSample(feature, source, replacement);
                feature.commandOwner = __instance;
                __instance.parameters = replacement;
                if (feature.DiagnosticsActive) feature.boostedCommands++;
            });
        }
        __state = sample;
    }

    public static void AfterMoveCommand(CharacterMovement __instance)
    {
        var feature = active;
        if (feature is null || !feature.DiagnosticsActive) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (!feature.IsLocal(__instance)) return;
            feature.lastRootRatio = __instance.animationRootMotionRatio;
            feature.lastDesiredSpeed = HorizontalLength(__instance._finalDirection);
        });
    }

    public static Exception? FinishMoveCommand(CharacterMovement __instance, CommandSample __state, Exception? __exception)
    {
        if (__state.Feature is not { } feature) return __exception;
        try
        {
            // Cleanup must run even when session gating has closed during the native call.
            if (__instance && __state.Original && __instance.parameters == __state.Replacement)
                __instance.parameters = __state.Original;
            feature.commandOwner = null;
        }
        catch (Exception cleanupError)
        {
            DiagnosticLog.LogError($"Movement parameter restoration failed: {cleanupError}");
            // Stop further boosted commands and let the host retry the outstanding owned restoration.
            feature.Hosted?.Disable();
            return __exception is null ? cleanupError : new AggregateException(__exception, cleanupError);
        }
        return __exception;
    }

    private void PrepareStats(MovementStats source)
    {
        if (!privateStats || sourceStats != source)
        {
            if (privateStats) NativeObject.Destroy(privateStats);
            privateStats = NativeObject.Instantiate(source).Cast<MovementStats>();
            sourceStats = source;
        }
        var copy = privateStats!;
        // Fields read by MoveCommand and its native ground/jump/water callees. No reflection in the hot path.
        copy.sprintMultiplier = source.sprintMultiplier;
        copy.walkBackPenakty = source.walkBackPenakty;
        copy.movementDeadzone = source.movementDeadzone;
        copy.commandChangeSpeed = source.commandChangeSpeed;
        copy.rotationSpeed = source.rotationSpeed;
        copy.groundMask = source.groundMask;
        copy.slideSpeed = source.slideSpeed;
        copy.sprintStaminaUsage = source.sprintStaminaUsage;
        copy.slideStairSpeed = source.slideStairSpeed;
        copy.heightDeltaCheck = source.heightDeltaCheck;
        copy.maxGroundHeight = source.maxGroundHeight;
        copy.maxUndergroundDepth = source.maxUndergroundDepth;
        copy.gravity = source.gravity;
        copy.maxFallVelocity = source.maxFallVelocity;
        copy.groundCheckDistance = source.groundCheckDistance;
        copy.groundCheckRadius = source.groundCheckRadius;
        copy.minSlideAgle = source.minSlideAgle;
        copy.slideHysterezisTime = source.slideHysterezisTime;
        copy.slideHysterezisAngle = source.slideHysterezisAngle;
        copy.slideStartTime = source.slideStartTime;
        copy.minFallAngle = source.minFallAngle;
        copy.maxGroundedDistance = source.maxGroundedDistance;
        copy.jumpForgivenessTime = source.jumpForgivenessTime;
        copy.swimEnterLevel = source.swimEnterLevel;
        copy.swimExitLevel = source.swimExitLevel;
        lastNativeSpeed = source.speed;
        lastAppliedSpeed = MovementDelta.ScaleNativeSpeed(lastNativeSpeed, Multiplier.Value);
        copy.speed = lastAppliedSpeed;
    }

    internal readonly record struct MotionSample(MotionVector Before, float Multiplier);

    public static void BeforeAnimatorMove(CharacterMovement __instance, out MotionSample __state)
    {
        var sample = default(MotionSample);
        var feature = active;
        if (feature is not null && feature.DiagnosticsActive) feature.animatorCalls++;
        if (feature is not null && feature.Multiplier.Value > 1)
            feature.Hosted?.TryExecute(() =>
            {
                if (!feature.IsLocal(__instance)) return;
                if (feature.DiagnosticsActive) feature.localAnimatorCalls++;
                if (!feature.CanScale(__instance, __instance.lastRawMovement)) return;
                if (feature.DiagnosticsActive) feature.eligibleAnimatorCalls++;
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
            if (feature.Multiplier.Value <= 1 || !feature.IsLocal(__instance)
                || !feature.CanScale(__instance, __instance.lastRawMovement)) return;
            var after = __instance._totalDeltaRootMotion;
            // Scale this callback's increment only; ASKA can accumulate several samples before consumption.
            var scaled = MovementDelta.ScaleHorizontal(__state.Before,
                new MotionVector(after.x, after.y, after.z), __state.Multiplier);
            __instance._totalDeltaRootMotion = new Vector3(scaled.X, scaled.Y, scaled.Z);
            if (feature.DiagnosticsActive)
            {
                var nativeDistance = Math.Sqrt(Math.Pow(after.x - __state.Before.X, 2) + Math.Pow(after.z - __state.Before.Z, 2));
                feature.nativeRootDistance += nativeDistance;
                feature.addedRootDistance += nativeDistance * (__state.Multiplier - 1);
                if (nativeDistance > 0) feature.scaledSamples++;
            }
        });
    }

    public static void BeforeApplyRootMotion(CharacterMovement __instance)
    {
        var feature = active;
        if (feature is null || !feature.DiagnosticsActive) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (!feature.IsLocal(__instance)) return;
            feature.applyCalls++;
            feature.consumedRootDistance += HorizontalLength(__instance._totalDeltaRootMotion);
        });
    }

    private static float HorizontalLength(Vector3 value) => MathF.Sqrt(value.x * value.x + value.z * value.z);

    public override void Tick()
    {
        if (!DiagnosticsActive || Stopwatch.GetTimestamp() < nextReport) return;
        reports++;
        StatusReason = $"Movement sample: {boostedCommands}/{localCommandCalls} local commands boosted; native speed {lastNativeSpeed:0.00} -> {lastAppliedSpeed:0.00}; root-motion share {lastRootRatio:0.00}.";
        DiagnosticLog.LogInfo($"Sample {reports}/12; multiplier={Multiplier.Value:0.0}; commands all/local/boosted={commandCalls}/{localCommandCalls}/{boostedCommands}; "
            + $"animator all/local/eligible/nonzero={animatorCalls}/{localAnimatorCalls}/{eligibleAnimatorCalls}/{scaledSamples}; "
            + $"root native/added/queued={nativeRootDistance:0.000}/{addedRootDistance:0.000}/{consumedRootDistance:0.000}; apply={applyCalls}; "
            + $"rootRatio={lastRootRatio:0.000}; native/appliedSpeed={lastNativeSpeed:0.000}/{lastAppliedSpeed:0.000}; desiredXZ={lastDesiredSpeed:0.000}; "
            + $"blocked air/raven/swim/climb/slide/cart/row/inputDisabled/idle={string.Join("/", blockedStates)}");
        ResetCounters();
        nextReport = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5;
    }

    private void ResetCounters()
    {
        animatorCalls = localAnimatorCalls = eligibleAnimatorCalls = scaledSamples = commandCalls = localCommandCalls = boostedCommands = applyCalls = 0;
        nativeRootDistance = addedRootDistance = consumedRootDistance = 0;
        Array.Clear(blockedStates, 0, blockedStates.Length);
    }

    public override void Disable()
    {
        active = null;
        harmony.UnpatchSelf();
        if (commandOwner && sourceStats && commandOwner!.parameters == privateStats) commandOwner.parameters = sourceStats;
        commandOwner = null;
        if (privateStats) NativeObject.Destroy(privateStats);
        privateStats = null;
        sourceStats = null;
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
