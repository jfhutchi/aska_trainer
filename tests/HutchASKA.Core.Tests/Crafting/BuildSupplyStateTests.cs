using HutchASKA.Core.Crafting;

namespace HutchASKA.Core.Tests.Crafting;

public sealed class BuildSupplyStateTests
{
    [Fact]
    public void UnfinishedPartRestoresOnlyTheInteractionStatesChangedBySupplyWaiver()
    {
        var original = new BuildSupplyState(false, true);
        Assert.Equal(original, original.Restore(false, true, false));
        Assert.Equal(new BuildSupplyState(false, true), original.Restore(false, false, true));
        Assert.Equal(new BuildSupplyState(true, false), original.Restore(true, true, false));
    }

    [Fact]
    public void ExistingBuildablePartIsNotReturnedToSupplyMode()
    {
        var original = new BuildSupplyState(true, false);
        Assert.Equal(original, original.Restore(false, true, false));
        Assert.Equal(new BuildSupplyState(false, true), original.Restore(false, false, true));
    }

    [Fact]
    public void SupplyReadAuthorizesOnlyOneMatchingNativeContainer()
    {
        var scope = new SupplyReadScope(42);
        Assert.False(scope.TryConsume(43));
        Assert.True(scope.TryConsume(42));
        Assert.False(scope.TryConsume(42));
        Assert.False(new SupplyReadScope(0).TryConsume(0));
    }
}
