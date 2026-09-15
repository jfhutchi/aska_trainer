using HutchASKA.Core.World;

namespace HutchASKA.Core.Tests.World;

public sealed class OneHourStepTests
{
    [Theory]
    [InlineData(12.5f, 1, 13.5f)]
    [InlineData(12.5f, -1, 11.5f)]
    [InlineData(1f, -1, 0f)]
    [InlineData(23f, 1, 24f)]
    [InlineData(23.5f, 1, 24.5f)]
    public void KeepsForwardDayCarryForTheNativeSetter(float hour, int direction, float expected)
    {
        Assert.True(OneHourStep.TryGetTarget(hour, direction, out var target, out var error));
        Assert.Equal(expected, target);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.5f)]
    [InlineData(.999f)]
    public void RejectsBackwardMidnightWithoutWrappingToLaterInTheSameDay(float hour)
    {
        Assert.False(OneHourStep.TryGetTarget(hour, -1, out var target, out var error));
        Assert.Equal(hour, target);
        Assert.Contains("midnight", error);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-1f)]
    [InlineData(24f)]
    public void InvalidNativeHoursAreRejected(float hour) =>
        Assert.False(OneHourStep.TryGetTarget(hour, 1, out _, out _));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void LargerOrEmptyJumpsAreRejected(int direction) =>
        Assert.False(OneHourStep.TryGetTarget(12, direction, out _, out _));
}
