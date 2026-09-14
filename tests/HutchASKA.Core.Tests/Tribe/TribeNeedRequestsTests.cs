using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class TribeNeedRequestsTests
{
    [Fact]
    public void RestoreNeedsExcludesHealthWarmthAndLifetime()
    {
        var request = TribeNeedRequests.RestoreAll;
        Assert.Equal(1, request.FoodFraction);
        Assert.Equal(1, request.WaterFraction);
        Assert.Equal(1, request.EnergyFraction);
        Assert.Equal(1, request.RestFraction);
        Assert.Equal(1, request.HappinessFraction);
        Assert.Null(request.HealthFraction);
        Assert.Null(request.WarmthFraction);
        Assert.Null(request.Age);
    }
}
