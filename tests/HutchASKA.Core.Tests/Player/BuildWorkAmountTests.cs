using HutchASKA.Core.Items;
using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class BuildWorkAmountTests
{
    [Theory]
    [InlineData(1, 2.5f)]
    [InlineData(2, 5)]
    [InlineData(3, 7.5f)]
    [InlineData(4, 10)]
    public void PresetsScaleOneNativeContribution(float multiplier, float expected) =>
        Assert.Equal(expected, BuildWorkAmount.Scale(2.5f, multiplier));

    [Fact]
    public void RestorationPreventsScalingTheNextSwingOrNativeCallerCosts()
    {
        var amount = 2f;
        for (var swing = 0; swing < 3; swing++)
        {
            var restore = new TemporaryFloatOverride(true, amount);
            amount = BuildWorkAmount.Scale(amount, 4);
            Assert.Equal(8, amount);
            restore.Restore(ref amount);
            Assert.Equal(2, amount);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void DoesNotGenerateWorkFromZeroOrScaleNegativeCorrections(float amount) =>
        Assert.Equal(amount, BuildWorkAmount.Scale(amount, 4));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void RejectsNonFiniteWork(float amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildWorkAmount.Scale(amount, 2));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(0)]
    [InlineData(5)]
    public void RejectsInvalidMultipliers(float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildWorkAmount.Scale(2, multiplier));

    [Fact]
    public void RejectsOverflowBeforePassingItToTheGame() =>
        Assert.Throws<InvalidOperationException>(() => BuildWorkAmount.Scale(float.MaxValue, 4));
}
