using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class FishingAssistMathTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void SpecialWeightScalesTheEntireNativeFormula(float multiplier)
    {
        var native = FishingAssistMath.EffectiveWeight(3, 2, .5f);
        var boosted = FishingAssistMath.EffectiveWeight(
            FishingAssistMath.ScaleWeight(3, true, multiplier),
            FishingAssistMath.ScaleWeight(2, true, multiplier), .5f);
        Assert.Equal(native * multiplier, boosted);
        Assert.Equal(3, FishingAssistMath.ScaleWeight(3, false, multiplier));
    }

    [Fact]
    public void NativeNormalizationIncreasesOddsWithoutGuaranteeingTheSpecialCatch()
    {
        const float common = 90;
        var special = FishingAssistMath.EffectiveWeight(4, 1, 0);
        var boosted = FishingAssistMath.EffectiveWeight(
            FishingAssistMath.ScaleWeight(4, true, 50), FishingAssistMath.ScaleWeight(1, true, 50), 0);
        Assert.Equal(50, (boosted / common) / (special / common), 4);
        Assert.InRange(boosted / (common + boosted), .73f, .74f);
        Assert.Equal(1, common / (common + boosted) + boosted / (common + boosted));
    }

    [Fact]
    public void ZeroWeightStaysZeroAndBaitOnlyWeightRemainsValid()
    {
        Assert.Equal(0, FishingAssistMath.ScaleWeight(0, true, 50));
        Assert.Equal(100, FishingAssistMath.EffectiveWeight(0, FishingAssistMath.ScaleWeight(2, true, 50), 5));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidWeightsFailBeforeNativeConfigurationChanges(float value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.ScaleWeight(value, true, 50));

    [Fact]
    public void OverflowAndInvalidMultipliersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.ScaleWeight(float.MaxValue, true, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.ScaleWeight(1, true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.RescaleTimer(1, 0, 1));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(20, 20)]
    [InlineData(30, 30)]
    [InlineData(40, 40)]
    [InlineData(50, 50)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(5, 1)]
    [InlineData(21, 1)]
    [InlineData(51, 1)]
    [InlineData(int.MaxValue, 1)]
    public void OnlyExplicitRarePresetsSurviveConfigurationReload(int configured, int expected) =>
        Assert.Equal(expected, FishingAssistMath.NormalizeRareWeightPreset(configured));

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(21)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void UnsupportedRareMultipliersAreRejected(float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.ScaleWeight(1, true, multiplier));

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void RareWeightPresetsCannotChangeTheBiteSpeedContract(float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FishingAssistMath.WaitTarget(20, multiplier));

    [Fact]
    public void WaitSpeedChangesPreserveProgressAndDisableRestoresNativeTiming()
    {
        var elapsed = 12f;
        var target = 20f;
        var fasterTarget = FishingAssistMath.WaitTarget(target, 2);
        elapsed = FishingAssistMath.RescaleTimer(elapsed, target, fasterTarget);
        Assert.Equal(.6f, elapsed / fasterTarget);
        var fastestTarget = FishingAssistMath.WaitTarget(target, 4);
        elapsed = FishingAssistMath.RescaleTimer(elapsed, fasterTarget, fastestTarget);
        Assert.Equal(3, elapsed);
        Assert.Equal(12, FishingAssistMath.RescaleTimer(elapsed, fastestTarget, target));
    }

    [Fact]
    public void EasyCatchDisableRestoresTheRemainingFractionWithoutRestartingWindow()
    {
        var extended = FishingAssistMath.RescaleTimer(3, 1, 4);
        Assert.Equal(12, extended);
        Assert.Equal(2, FishingAssistMath.RescaleTimer(extended - 4, 4, 1));
        Assert.Equal(0, FishingAssistMath.RescaleTimer(0, 4, 1));
    }
}
