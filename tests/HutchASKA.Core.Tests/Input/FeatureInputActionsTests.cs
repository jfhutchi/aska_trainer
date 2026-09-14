using HutchASKA.Core.Compatibility;
using HutchASKA.Core.Features;
using HutchASKA.Core.Input;

namespace HutchASKA.Core.Tests.Input;

public sealed class FeatureInputActionsTests
{
    [Fact]
    public void OpenMenuRecoversWhenSessionBecomesAvailableAndDoesNotDuplicateEnable()
    {
        var mode = SessionMode.Unknown;
        var native = new Stub();
        var feature = Host(native, () => mode);
        FeatureInputActions.EnsureEnabled(feature);
        Assert.Equal(FeatureState.Blocked, feature.State);
        Assert.Equal(0, native.EnableCount);
        mode = SessionMode.SinglePlayer;
        FeatureInputActions.EnsureEnabled(feature);
        FeatureInputActions.EnsureEnabled(feature);
        Assert.Equal(FeatureState.Enabled, feature.State);
        Assert.Equal(1, native.EnableCount);
        mode = SessionMode.Unknown;
        feature.Tick();
        Assert.Equal(1, native.DisableCount);
        mode = SessionMode.SinglePlayer;
        FeatureInputActions.EnsureEnabled(feature);
        Assert.Equal(2, native.EnableCount);
    }

    [Fact]
    public void HotkeyCanReactivateBlockedFeatureAfterSessionRecovery()
    {
        var mode = SessionMode.SinglePlayer;
        var native = new Stub();
        var feature = Host(native, () => mode);
        Assert.True(FeatureInputActions.TryToggle(feature));
        mode = SessionMode.Multiplayer;
        feature.Tick();
        Assert.False(FeatureInputActions.TryToggle(feature));
        Assert.Equal(1, native.EnableCount);
        mode = SessionMode.SinglePlayer;
        Assert.True(FeatureInputActions.TryToggle(feature));
        Assert.Equal(2, native.EnableCount);
        Assert.True(FeatureInputActions.TryToggle(feature));
        Assert.Equal(FeatureState.Disabled, feature.State);
    }

    [Fact]
    public void FailedEnableRestoresOnceAndInputCannotRetryFaultedFeature()
    {
        var native = new Stub { FailEnable = true };
        var feature = Host(native, () => SessionMode.SinglePlayer);
        FeatureInputActions.EnsureEnabled(feature);
        Assert.Equal(FeatureState.Faulted, feature.State);
        FeatureInputActions.EnsureEnabled(feature);
        Assert.False(FeatureInputActions.TryToggle(feature));
        Assert.Equal(1, native.EnableCount);
        Assert.Equal(1, native.DisableCount);
    }

    private static HostedFeature Host(Stub native, Func<SessionMode> mode) =>
        new(native, () => SinglePlayerDecision.Evaluate(mode()), (_, _) => { });

    private sealed class Stub : ITrainerFeature
    {
        public string Id => "input-test";
        public string DisplayName => Id;
        public FeatureState State => FeatureState.Disabled;
        public string? StatusReason => null;
        public bool FailEnable { get; init; }
        public int EnableCount { get; private set; }
        public int DisableCount { get; private set; }
        public CompatibilityResult ProbeCompatibility() => CompatibilityResult.Compatible();
        public bool TryEnable()
        {
            EnableCount++;
            if (FailEnable) throw new InvalidOperationException("partial native enable");
            return true;
        }
        public void Disable() => DisableCount++;
        public void Reset() => Disable();
        public void Tick() { }
    }
}
