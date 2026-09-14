using HutchASKA.Core.Features;

namespace HutchASKA.Core.Tests.Features;

public sealed class FeatureRegistryTests
{
    [Fact]
    public void Register_RejectsDuplicateIgnoringCase()
    {
        var registry = new FeatureRegistry();
        registry.Register(new FakeFeature("god-mode"));
        Assert.Contains("GOD-MODE", Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new FakeFeature("GOD-MODE"))).Message);
    }

    [Fact]
    public void Snapshot_IsOrderedAndDetachedFromFutureRegistrations()
    {
        var registry = new FeatureRegistry();
        var god = new FakeFeature("god-mode");
        registry.Register(god);
        var snapshot = registry.Snapshot();
        registry.Register(new FakeFeature("stamina"));
        Assert.Single(snapshot);
        Assert.Equal(new[] { "god-mode", "stamina" }, registry.Snapshot().Select(f => f.Id));
        Assert.Same(god, registry.Find("GOD-MODE"));
        Assert.Null(registry.Find("absent"));
        Assert.Equal(FeatureState.Disabled, god.State);
    }

    private sealed class FakeFeature(string id) : ITrainerFeature
    {
        public string Id => id;
        public string DisplayName => id;
        public FeatureState State => FeatureState.Disabled;
        public string? StatusReason => null;
        public CompatibilityResult ProbeCompatibility() => CompatibilityResult.Compatible();
        public bool TryEnable() => false;
        public void Disable() { }
        public void Reset() { }
        public void Tick() { }
    }
}
