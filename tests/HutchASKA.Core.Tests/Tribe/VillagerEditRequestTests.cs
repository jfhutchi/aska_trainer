using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class VillagerEditRequestTests
{
    [Fact]
    public void EmptyRequestIsANoopAndRetainsNullFields()
    {
        var request = new VillagerEditRequest();
        Assert.True(request.IsEmpty);
        Assert.Equal(request, request.Clamp());
    }

    [Fact]
    public void FractionsClampWithoutAddingUnrequestedEdits()
    {
        var request = new VillagerEditRequest(HealthFraction: -1, FoodFraction: 2, HappinessFraction: .7f).Clamp();
        Assert.False(request.IsEmpty);
        Assert.Equal(0, request.HealthFraction);
        Assert.Equal(1, request.FoodFraction);
        Assert.Equal(.7f, request.HappinessFraction);
        Assert.Null(request.WaterFraction);
        Assert.Null(request.Age);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteEditsAreRejected(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VillagerEditRequest(RestFraction: value).Clamp());
        Assert.Throws<ArgumentOutOfRangeException>(() => new VillagerEditRequest(Age: value).Clamp());
    }
}
