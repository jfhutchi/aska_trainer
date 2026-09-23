using HutchASKA.Core.Crafting;

namespace HutchASKA.Core.Tests.Crafting;

public sealed class BenchCraftProgressTests
{
    [Theory]
    [InlineData(0f, 1f, 10f, 3f)]
    [InlineData(5f, 6f, 10f, 8f)]
    [InlineData(8f, 9f, 10f, 10f)]
    public void TriplesOnlyTheNativeIncrement(float before, float after, float target, float expected) =>
        Assert.Equal(expected, BenchCraftProgress.TripleIncrement(before, after, target));

    [Theory]
    [InlineData(4f, 4f, 10f)]
    [InlineData(4f, 0f, 10f)]
    [InlineData(9f, 10f, 10f)]
    [InlineData(0f, 1f, 0f)]
    public void LeavesIdleResetAndCompletedProgressAlone(float before, float after, float target) =>
        Assert.Equal(after, BenchCraftProgress.TripleIncrement(before, after, target));

    [Theory]
    [InlineData(float.NaN, 1f, 10f)]
    [InlineData(0f, float.PositiveInfinity, 10f)]
    [InlineData(0f, 1f, float.NaN)]
    public void RejectsInvalidNativeProgress(float before, float after, float target) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BenchCraftProgress.TripleIncrement(before, after, target));
}
