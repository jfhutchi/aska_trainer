using HutchASKA.Core.Compatibility;
using HutchASKA.Core.Features;

namespace HutchASKA.Core.Tests.Features;

public sealed class CompatibilityRefreshTests
{
    [Fact]
    public void RescanDisablesBeforeProbingAndDoesNotReactivate()
    {
        var native = new Stub();
        var hosted = Host(native);
        Assert.True(hosted.TryEnable());
        Assert.True(hosted.RefreshCompatibility().IsCompatible);
        Assert.Equal(new[] { "probe", "enable", "disable", "probe" }, native.Calls);
        Assert.Equal(FeatureState.Disabled, hosted.State);
        Assert.True(hosted.TryEnable());
        Assert.Equal(2, native.Calls.Count(c => c == "enable"));
    }
    [Fact]
    public void RescanCanRecoverAnIncompatibleProbeWithoutEnabling()
    {
        var native = new Stub { Compatible = false };
        var hosted = Host(native);
        Assert.False(hosted.ProbeCompatibility().IsCompatible);
        native.Compatible = true;
        Assert.True(hosted.RefreshCompatibility().IsCompatible);
        Assert.Equal(FeatureState.Disabled, hosted.State);
        Assert.DoesNotContain("enable", native.Calls);
    }
    [Fact]
    public void PendingCleanupBlocksProbeAndFaultRemainsAfterExplicitRecovery()
    {
        var native = new Stub { FailCleanup = true };
        var hosted = Host(native);
        Assert.True(hosted.TryEnable());
        hosted.Disable();
        Assert.True(hosted.HasPendingCleanup);
        Assert.False(hosted.RefreshCompatibility().IsCompatible);
        Assert.Equal(1, native.Calls.Count(c => c == "probe"));
        native.FailCleanup = false;
        Assert.False(hosted.RefreshCompatibility().IsCompatible);
        Assert.False(hosted.HasPendingCleanup);
        Assert.Equal(FeatureState.Faulted, hosted.State);
        Assert.False(hosted.TryEnable());
    }
    [Fact]
    public void ResetAllResetsControllerMultiplierEvenWhenFeatureIsFaulted()
    {
        var native = new Stub { FailCleanup = true };
        var hosted = Host(native);
        hosted.TryEnable();
        hosted.Disable();
        var multiplier = new MultiplierSetting(1, 5) { Value = 4 };
        FeatureReset.ResetAll(new[] { hosted }, multiplier);
        Assert.Equal(1, multiplier.Value);
        Assert.Equal(FeatureState.Faulted, hosted.State);
        Assert.True(hosted.HasPendingCleanup);
    }
    private static HostedFeature Host(Stub native) => new(native,
        () => SinglePlayerDecision.Evaluate(SessionMode.SinglePlayer), (_, _) => { });
    private sealed class Stub : ITrainerFeature
    {
        public string Id => "refresh";
        public string DisplayName => Id;
        public FeatureState State => FeatureState.Disabled;
        public string? StatusReason => null;
        public bool Compatible { get; set; } = true;
        public bool FailCleanup { get; set; }
        public List<string> Calls { get; } = new();
        public CompatibilityResult ProbeCompatibility() { Calls.Add("probe"); return Compatible ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("missing"); }
        public bool TryEnable() { Calls.Add("enable"); return true; }
        public void Disable() { Calls.Add("disable"); if (FailCleanup) throw new InvalidOperationException("cleanup"); }
        public void Reset() => Disable();
        public void Tick() { }
    }
}
