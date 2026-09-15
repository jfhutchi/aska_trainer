using System.Numerics;
using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class MovementDeltaTests
{
    [Fact]
    public void ScalesOnlyTheNewHorizontalDistanceAndPreservesVerticalMotion()
    {
        var before = new Vector3(3, 4, 5);
        var after = new Vector3(4, 6, 3);
        Assert.Equal(new Vector3(5, 6, 1), MovementDelta.ScaleHorizontal(before, after, 2));
    }

    [Fact]
    public void RepeatedAnimationSamplesDoNotMultiplyPreviouslyAccumulatedDistance()
    {
        var accumulated = MovementDelta.ScaleHorizontal(Vector3.Zero, new Vector3(1, 0, 0), 5);
        accumulated = MovementDelta.ScaleHorizontal(accumulated, accumulated + new Vector3(1, 0, 0), 5);
        Assert.Equal(new Vector3(10, 0, 0), accumulated);
        Assert.Equal(accumulated, MovementDelta.ScaleHorizontal(accumulated, accumulated, 5));
    }

    [Fact]
    public void NativeSpeedPreservesTheExactSample()
    {
        var after = new Vector3(.01f, -.5f, -.02f);
        Assert.Equal(after, MovementDelta.ScaleHorizontal(new Vector3(100, 100, 100), after, 1));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(6)]
    public void RejectsInvalidMultiplier(float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MovementDelta.ScaleHorizontal(Vector3.Zero, Vector3.One, multiplier));

    [Fact]
    public void RejectsNonFiniteNativeMotion() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MovementDelta.ScaleHorizontal(Vector3.Zero, new Vector3(float.NaN, 0, 0), 2));
}
