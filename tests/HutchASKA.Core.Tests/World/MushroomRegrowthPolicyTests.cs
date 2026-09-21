using HutchASKA.Core.World;

namespace HutchASKA.Core.Tests.World;

public sealed class MushroomRegrowthPolicyTests
{
    [Theory]
    [InlineData(0x01004008, "Item_Food_BiomeMushroom1")]
    [InlineData(0x01004009, "Item_Food_BiomeMushroomGrey")]
    [InlineData(0x0100400a, "Item_Food_BiomeMushroomYellow")]
    public void RequiresBothExactNativeIdAndAssetName(int id, string name)
    {
        Assert.True(MushroomRegrowthPolicy.IsMushroom(id, name));
        Assert.False(MushroomRegrowthPolicy.IsMushroom(id + 1, name));
        Assert.False(MushroomRegrowthPolicy.IsMushroom(id, name + "Cooked"));
        Assert.False(MushroomRegrowthPolicy.IsMushroom(id, null));
    }

    [Theory]
    [InlineData(2, 11)]
    [InlineData(4, 10.5f)]
    public void AvailabilityOnlyModeUsesExplicitTwoDayReference(float multiplier, float expected) =>
        Assert.Equal(expected, MushroomRegrowthPolicy.Target(10, -1, -1, true, false, multiplier));

    [Fact]
    public void FiniteTimerAcceleratesWithoutPostponingSoonerSeasonOrOverdueDates()
    {
        Assert.Equal(11, MushroomRegrowthPolicy.Target(10, 14, 4, true, false, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, 10.5f, 4, true, true, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, 9, 4, true, false, 4));
    }

    [Fact]
    public void NeverSeasonOnlyNormalAndInvalidSchedulesRemainNative()
    {
        Assert.Null(MushroomRegrowthPolicy.Target(10, -1, -1, false, false, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, 20, -1, true, true, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, -1, 0, true, false, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, -1, -1, true, false, 1));
        Assert.Null(MushroomRegrowthPolicy.Target(float.NaN, -1, -1, true, false, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(10, float.PositiveInfinity, 2, true, false, 4));
        Assert.Null(MushroomRegrowthPolicy.Target(float.MaxValue, -1, -1, true, false, 4));
    }

    [Fact]
    public void RestorationRequiresExactLiveWorldClockResourceDataAndUnconsumedDeadline()
    {
        var lease = new MushroomScheduleLease(1, 2, 3, 4, -1, 10.5f);
        Assert.True(lease.Owns(1, 2, 3, 4, 10.5f, false));
        Assert.False(lease.Owns(9, 2, 3, 4, 10.5f, false));
        Assert.False(lease.Owns(1, 9, 3, 4, 10.5f, false));
        Assert.False(lease.Owns(1, 2, 9, 4, 10.5f, false));
        Assert.False(lease.Owns(1, 2, 3, 9, 10.5f, false));
        Assert.False(lease.Owns(1, 2, 3, 4, 11, false));
        Assert.False(lease.Owns(1, 2, 3, 4, 10.5f, true));
    }
}
