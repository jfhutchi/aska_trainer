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

    [Fact]
    public void ReusedSnapshotCannotBeMutatedByCaller()
    {
        var registry = new FeatureRegistry();
        var feature = new FakeFeature("food");
        registry.Register(feature);
        var snapshot = registry.Snapshot();
        var mutable = Assert.IsAssignableFrom<IList<ITrainerFeature>>(snapshot);
        Assert.Throws<NotSupportedException>(() => mutable[0] = new FakeFeature("water"));
        Assert.Same(snapshot, registry.Snapshot());
        Assert.Same(feature, registry.Snapshot()[0]);
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
