using HutchASKA.Core.Compatibility;
using HutchASKA.Core.Features;

namespace HutchASKA.Core.Tests.Features;

public sealed class HostedFeatureTests
{
    [Fact]
    public void MissingSignature_IsIncompatibleAndDoesNotEnable()
    {
        var feature = new FakeFeature { ProbeError = new MissingMethodException("Missing target") };
        var errors = new List<Exception>();
        var hosted = Host(feature, errors);
        Assert.False(hosted.ProbeCompatibility().IsCompatible);
        Assert.Equal(FeatureState.Incompatible, hosted.State);
        Assert.Contains("Missing target", hosted.StatusReason);
        Assert.False(hosted.TryEnable());
        Assert.Single(errors);
    }

    [Fact]
    public void Gate_BlocksEnableAndRestoresActiveFeatureOnSessionLoss()
    {
        var mode = SessionMode.Unknown;
        var feature = new FakeFeature();
        var hosted = new HostedFeature(feature, () => SinglePlayerDecision.Evaluate(mode), (_, _) => { });
        Assert.False(hosted.TryEnable());
        Assert.Equal(FeatureState.Blocked, hosted.State);
        Assert.Equal("Single-player state not confirmed", hosted.StatusReason);
        mode = SessionMode.SinglePlayer;
        Assert.True(hosted.TryEnable());
        mode = SessionMode.Multiplayer;
        hosted.Tick();
        Assert.Equal(1, feature.DisableCount);
        Assert.Equal(0, feature.TickCount);
        Assert.Equal(FeatureState.Blocked, hosted.State);
        Assert.Equal("Multiplayer/co-op session detected", hosted.StatusReason);
    }

    [Fact]
    public void RepeatedTickFailure_FaultsOnlyThatFeatureAndRestoresOnce()
    {
        var failed = new FakeFeature { TickError = new InvalidOperationException("Tick failed") };
        var errors = new List<Exception>();
        var hosted = Host(failed, errors);
        var healthy = new FakeFeature();
        var other = Host(healthy, new());
        Assert.True(hosted.TryEnable());
        Assert.True(other.TryEnable());
        for (var i = 0; i < 5; i++) { hosted.Tick(); other.Tick(); }
        Assert.Equal(FeatureState.Faulted, hosted.State);
        Assert.Equal(3, failed.TickCount);
        Assert.Equal(1, failed.DisableCount);
        Assert.Equal(3, errors.Count);
        Assert.Equal(5, healthy.TickCount);
        Assert.False(hosted.TryEnable());
    }

    [Fact]
    public void RestorationFailure_IsVisibleAndDoesNotRetryEveryFrame()
    {
        var mode = SessionMode.SinglePlayer;
        var feature = new FakeFeature { DisableError = new InvalidOperationException("Restore failed") };
        var errors = new List<Exception>();
        var hosted = new HostedFeature(feature, () => SinglePlayerDecision.Evaluate(mode), (_, error) => errors.Add(error));
        Assert.True(hosted.TryEnable());
        mode = SessionMode.Unknown;
        hosted.Tick();
        hosted.Tick();
        Assert.Equal(FeatureState.Faulted, hosted.State);
        Assert.Contains("Restore failed", hosted.StatusReason);
        Assert.Equal(1, feature.DisableCount);
        Assert.Single(errors);
    }

    private static HostedFeature Host(FakeFeature feature, List<Exception> errors) =>
        new(feature, () => SinglePlayerDecision.Evaluate(SessionMode.SinglePlayer), (_, error) => errors.Add(error));

    [Fact]
    public void ExplicitResetRetriesFailedNativeCleanupWithoutTickRetry()
    {
        var feature = new FakeFeature { DisableError = new InvalidOperationException("restore") };
        var hosted = Host(feature, new());
        Assert.True(hosted.TryEnable());
        hosted.Disable();
        hosted.Tick();
        Assert.Equal(1, feature.DisableCount);
        feature.DisableError = null;
        hosted.Reset();
        Assert.Equal(2, feature.DisableCount);
        hosted.Disable();
        Assert.Equal(2, feature.DisableCount);
        Assert.Equal(FeatureState.Faulted, hosted.State);
    }

    private sealed class FakeFeature : ITrainerFeature
    {
        public string Id => "test";
        public string DisplayName => "Test feature";
        public FeatureState State { get; private set; }
        public string? StatusReason => null;
        public Exception? ProbeError { get; init; }
        public Exception? TickError { get; init; }
        public Exception? DisableError { get; set; }
        public int TickCount { get; private set; }
        public int DisableCount { get; private set; }
        public CompatibilityResult ProbeCompatibility() => ProbeError is { } error ? throw error : CompatibilityResult.Compatible();
        public bool TryEnable() { State = FeatureState.Enabled; return true; }
        public void Disable()
        {
            DisableCount++;
            if (DisableError is { } error) throw error;
            State = FeatureState.Disabled;
        }
        public void Reset() => Disable();
        public void Tick() { TickCount++; if (TickError is { } error) throw error; }
    }
}
