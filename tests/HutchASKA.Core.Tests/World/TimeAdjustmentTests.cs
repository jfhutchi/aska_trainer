using HutchASKA.Core.World;

namespace HutchASKA.Core.Tests.World;

public sealed class TimeAdjustmentTests
{
    [Theory]
    [InlineData(23.5, 1, 24, .5)]
    [InlineData(.5, -1, 24, 23.5)]
    [InlineData(.9, .2, 1, .1)]
    public void WrapsInExplicitCallerUnits(double current, double delta, double period, double expected) =>
        Assert.Equal(expected, TimeAdjustment.Wrap(current, delta, period), 6);
    [Fact]
    public void RejectsUnknownPeriod() => Assert.Throws<ArgumentOutOfRangeException>(() => TimeAdjustment.Wrap(1, 1, 0));
}
