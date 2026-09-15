using HutchASKA.Core.Items;

namespace HutchASKA.Core.Tests.Items;

public sealed class ConsumableRetentionScopeTests
{
    [Fact]
    public void EffectsMustFinishBeforeExactlyOneRemovalCanBeRetained()
    {
        var scope = new ConsumableRetentionScope(10, 20);
        Assert.False(scope.TryRetain(10, 20, 1, true));
        scope.EffectsApplied(10);
        Assert.True(scope.TryRetain(10, 20, 1, true));
        Assert.False(scope.TryRetain(10, 20, 1, true));
    }

    [Fact]
    public void OtherEffectsAndOtherItemsCannotConsumeThisAuthorization()
    {
        var scope = new ConsumableRetentionScope(10, 20);
        scope.EffectsApplied(11);
        Assert.False(scope.TryRetain(10, 20, 1, true));
        scope.EffectsApplied(10);
        Assert.False(scope.TryRetain(11, 20, 1, true));
        Assert.False(scope.TryRetain(10, 21, 1, true));
        Assert.False(scope.TryRetain(10, 20, 2, true));
        Assert.False(scope.TryRetain(10, 20, 1, false));
        Assert.True(scope.TryRetain(10, 20, 1, true));
    }

    [Fact]
    public void SeparateNestedUsesKeepIndependentItemAndContainerAuthorization()
    {
        var outer = new ConsumableRetentionScope(10, 20);
        var inner = new ConsumableRetentionScope(11, 20);
        outer.EffectsApplied(10);
        inner.EffectsApplied(11);
        Assert.False(inner.TryRetain(10, 20, 1, true));
        Assert.True(inner.TryRetain(11, 20, 1, true));
        Assert.True(outer.TryRetain(10, 20, 1, true));
    }

    [Fact]
    public void MissingNativeIdentitiesCannotAuthorizeARemoval()
    {
        var scope = new ConsumableRetentionScope(0, 0);
        scope.EffectsApplied(0);
        Assert.False(scope.TryRetain(0, 0, 1, true));
    }
}
