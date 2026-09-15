using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class TerrainAreaSettingTests
{
    [Fact]
    public void DefaultsAndResetsToNormalFiveTiles()
    {
        var setting = new TerrainAreaSetting();
        Assert.Equal(5, setting.Value);
        setting.Value = 20;
        setting.Reset();
        Assert.Equal(5, setting.Value);
    }

    [Theory]
    [InlineData(int.MinValue, 5)]
    [InlineData(10, 10)]
    [InlineData(15, 15)]
    [InlineData(int.MaxValue, 20)]
    public void BoundsRequestedSize(int requested, int expected)
    {
        var setting = new TerrainAreaSetting { Value = requested };
        Assert.Equal(expected, setting.Value);
    }

    [Theory]
    [InlineData(20, 3, 15)]
    [InlineData(20, 6, 20)]
    [InlineData(10, 3, 10)]
    [InlineData(20, 0, 5)]
    public void RespectsActualPackedNativeCapacity(int requested, int elements, int expected) =>
        Assert.Equal(expected, TerrainAreaSetting.MaximumSide(requested, elements));

    [Theory]
    [InlineData(400, 1, false)]
    [InlineData(-400, 1, false)]
    [InlineData(1, -400, false)]
    [InlineData(-20, -20, true)]
    [InlineData(20, 20, true)]
    [InlineData(int.MinValue, 1, false)]
    public void RejectsOversizedAxesWithoutChangingTheNativeRectangle(int x, int z, bool expected)
    {
        Assert.Equal(expected, TerrainAreaSetting.Fits(x, z, 20));
    }
}
