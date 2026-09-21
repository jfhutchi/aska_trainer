using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class SkillGainMathTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ScalesEarnedExperienceBeforeNativeBonuses(float multiplier)
    {
        const float earned = 0.25f, proficiencyBonus = 1.5f;
        Assert.Equal(earned * proficiencyBonus * multiplier,
            SkillGainMath.ScaleAward(earned, multiplier) * proficiencyBonus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void DoesNotCreateExperienceOrMultiplyLosses(float amount) =>
        Assert.Equal(amount, SkillGainMath.ScaleAward(amount, 5));

    [Fact]
    public void ReturningToNormalPreservesNewAward()
    {
        Assert.Equal(2.5f, SkillGainMath.ScaleAward(0.5f, 5));
        Assert.Equal(0.5f, SkillGainMath.ScaleAward(0.5f, 1));
    }

    [Theory]
    [InlineData(float.NaN, 2)]
    [InlineData(float.PositiveInfinity, 2)]
    [InlineData(1, float.NaN)]
    [InlineData(1, float.PositiveInfinity)]
    [InlineData(1, 0)]
    [InlineData(1, 6)]
    public void RejectsInvalidValues(float amount, float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SkillGainMath.ScaleAward(amount, multiplier));

    [Fact]
    public void RejectsOverflowBeforeAwardModification() =>
        Assert.Throws<InvalidOperationException>(() => SkillGainMath.ScaleAward(float.MaxValue, 5));
}
