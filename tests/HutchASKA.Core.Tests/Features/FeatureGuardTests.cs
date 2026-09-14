using HutchASKA.Core.Compatibility;
using HutchASKA.Core.Features;

namespace HutchASKA.Core.Tests.Features;

public sealed class FeatureGuardTests
{
    [Fact]
    public void CallbackRefreshesGateAndBlocksBeforeNativeAction()
    {
        var mode = SessionMode.SinglePlayer;
        var hosted = new HostedFeature(new Stub(), () => SinglePlayerDecision.Evaluate(mode), (_, _) => { });
        Assert.True(hosted.TryEnable());
        mode = SessionMode.Multiplayer;
        var ran = false;
        Assert.False(hosted.TryExecute(() => ran = true));
        Assert.False(ran);
        Assert.Equal(FeatureState.Blocked, hosted.State);
        Assert.Equal("Multiplayer/co-op session detected", hosted.StatusReason);
    }

    [Fact]
    public void CallbackFaultTripsOnlyItsHostAndDefaultsToNativeBehavior()
    {
        var hosted = new HostedFeature(new Stub(), () => SinglePlayerDecision.Evaluate(SessionMode.SinglePlayer), (_, _) => { });
        Assert.True(hosted.TryEnable());
        for (var i = 0; i < 4; i++) Assert.False(hosted.TryExecute(() => throw new InvalidOperationException("patch")));
        Assert.Equal(FeatureState.Faulted, hosted.State);
    }

    private sealed class Stub : ITrainerFeature
    {
        public string Id => "guard";
        public string DisplayName => Id;
        public FeatureState State => FeatureState.Disabled;
        public string? StatusReason => null;
        public CompatibilityResult ProbeCompatibility() => CompatibilityResult.Compatible();
        public bool TryEnable() => true;
        public void Disable() { }
        public void Reset() { }
        public void Tick() { }
    }
}
