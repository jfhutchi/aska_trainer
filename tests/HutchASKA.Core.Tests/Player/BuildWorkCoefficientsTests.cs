using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class BuildWorkCoefficientsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ScalesWholeWorkFormulaWithoutChangingTimeOrToolBonus(float multiplier)
    {
        var native = new BuildWorkCoefficients(2, 3);
        var boosted = native.Scale(multiplier);
        const float attributes = 5, time = 2, toolBonus = 1.5f;
        var originalWork = (attributes * native.AttributeMultiplier + native.BaseWork) * time * toolBonus;
        var boostedWork = (attributes * boosted.AttributeMultiplier + boosted.BaseWork) * time * toolBonus;
        Assert.Equal(originalWork * multiplier, boostedWork);
        Assert.Equal(new BuildWorkCoefficients(2, 3), native);
    }

    [Theory]
    [InlineData(-2, 3)]
    [InlineData(2, -3)]
    [InlineData(0, 0)]
    public void PreservesSignedCoefficientsAndZero(float baseline, float attributeMultiplier)
    {
        var boosted = new BuildWorkCoefficients(baseline, attributeMultiplier).Scale(4);
        Assert.Equal(baseline * 4, boosted.BaseWork);
        Assert.Equal(attributeMultiplier * 4, boosted.AttributeMultiplier);
    }

    [Fact]
    public void RepeatedEventsUseTheOriginalCoefficients()
    {
        var native = new BuildWorkCoefficients(2, 3);
        Assert.Equal(new BuildWorkCoefficients(8, 12), native.Scale(4));
        Assert.Equal(new BuildWorkCoefficients(4, 6), native.Scale(2));
        Assert.Equal(native, native.Scale(1));
    }

    [Theory]
    [InlineData(float.NaN, 1, 2)]
    [InlineData(1, float.PositiveInfinity, 2)]
    [InlineData(1, 1, float.NaN)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 5)]
    public void RejectsInvalidInputs(float baseline, float attributeMultiplier, float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BuildWorkCoefficients(baseline, attributeMultiplier).Scale(multiplier));

    [Fact]
    public void RejectsOverflowBeforeNativeConfigurationIsChanged() =>
        Assert.Throws<InvalidOperationException>(() => new BuildWorkCoefficients(float.MaxValue, 1).Scale(4));
}
