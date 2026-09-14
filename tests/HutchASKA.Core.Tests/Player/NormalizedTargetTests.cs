using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class NormalizedTargetTests
{
    [Theory]
    [InlineData(0, 100, 1, 100)]
    [InlineData(0, 100, .5f, 50)]
    [InlineData(20, 80, 0, 20)]
    [InlineData(20, 80, -2, 20)]
    [InlineData(20, 80, 2, 80)]
    public void MapsClampedFraction(float min, float max, float fraction, float expected) =>
        Assert.Equal(expected, AttributeMath.ValueAtFraction(min, max, fraction));

    [Fact]
    public void RejectsInvalidNativeRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeMath.ValueAtFraction(5, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeMath.ValueAtFraction(0, float.NaN, 1));
    }
}
