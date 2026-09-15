using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class TribeNeedsBatchTests
{
    [Fact]
    public void FailedSharedPassReachesEveryParticipantWithoutRepeatingNativeWork()
    {
        var batch = new TribeNeedsBatch();
        batch.Set("food", new(FoodFraction: 1));
        batch.Set("rest", new(RestFraction: 1));
        Assert.True(batch.TryTake(1000, out _));
        var failure = new InvalidOperationException("Rest attribute is invalid.");
        batch.RecordFailure(failure);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => batch.TryTake(1000, out _)).InnerException);
        batch.Remove("food");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => batch.TryTake(1001, out _)).InnerException);
        batch.Remove("rest");
        batch.Set("water", new(WaterFraction: 1));
        Assert.True(batch.TryTake(1002, out var request));
        Assert.Equal(new VillagerEditRequest(WaterFraction: 1), request);
    }

    [Fact]
    public void EnabledNeedsProduceOneCombinedPassPerInterval()
    {
        var batch = new TribeNeedsBatch();
        batch.Set("food", new(FoodFraction: 1));
        batch.Set("water", new(WaterFraction: 1));
        Assert.True(batch.TryTake(1000, out var request));
        Assert.Equal(new VillagerEditRequest(FoodFraction: 1, WaterFraction: 1), request);
        for (var i = 0; i < 10; i++) Assert.False(batch.TryTake(1000, out _));
        Assert.False(batch.TryTake(1499, out _));
        Assert.True(batch.TryTake(1500, out _));
    }

    [Fact]
    public void DisablingOneNeedPreservesOthersAndDisablingAllStopsWork()
    {
        var batch = new TribeNeedsBatch();
        batch.Set("food", new(FoodFraction: 1));
        batch.Set("water", new(WaterFraction: 1));
        batch.Remove("food");
        Assert.True(batch.TryTake(10, out var request));
        Assert.Null(request.FoodFraction);
        Assert.Equal(1f, request.WaterFraction);
        batch.Remove("water");
        Assert.False(batch.TryTake(100000, out _));
    }

    [Fact]
    public void ChangedSettingsAreAppliedWithoutWaitingForOldDeadline()
    {
        var batch = new TribeNeedsBatch();
        batch.Set("food", new(FoodFraction: 1));
        Assert.True(batch.TryTake(1000, out _));
        batch.Set("rest", new(RestFraction: 1));
        Assert.True(batch.TryTake(1001, out var request));
        Assert.Equal(1f, request.RestFraction);
        Assert.Null(request.HealthFraction);
        Assert.Null(request.WarmthFraction);
        Assert.Null(request.Age);
    }
}
