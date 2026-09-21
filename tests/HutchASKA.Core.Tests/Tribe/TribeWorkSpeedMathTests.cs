using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class TribeWorkSpeedMathTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void BuildScalesBothCoefficientsWithoutChangingElapsedTime(float multiplier)
    {
        var work = (TribeWorkSpeedMath.ScaleCoefficient(2, multiplier)
            + 3 * TribeWorkSpeedMath.ScaleCoefficient(4, multiplier)) * 0.5f;
        Assert.Equal(7 * multiplier, work);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(10, 2, 1)]
    [InlineData(10, 2, 0.5f)]
    public void HarvestScalesWholeNativeWorkWhilePreservingItsMultiplier(float weaponDamage, float bonus, float nativeMultiplier)
    {
        const float baseline = 10, coefficient = 3;
        var nativeWork = (1 + bonus * coefficient) * weaponDamage * nativeMultiplier + baseline;
        foreach (var multiplier in new[] { 1f, 2f, 3f, 4f, 5f })
        {
            var boostedBase = TribeWorkSpeedMath.HarvestBaseDamage(baseline, weaponDamage, bonus, coefficient, nativeMultiplier, multiplier);
            var boostedWork = (1 + bonus * coefficient) * weaponDamage * nativeMultiplier + boostedBase;
            Assert.Equal(nativeWork * multiplier, boostedWork);
        }
    }

    [Fact]
    public void GatherCountsOnlyAdditionalWorkAndLeavesCompletionToNativeUpdate()
    {
        var remaining = TribeWorkSpeedMath.GatherRemaining(20, 2, 3, 4, 0.25f, 5);
        Assert.Equal(6, remaining);
        Assert.Equal(2.5f, remaining - (2 + 3 * 4) * 0.25f);
        Assert.Equal(-13, TribeWorkSpeedMath.GatherRemaining(1, 2, 3, 4, 0.25f, 5));
        Assert.Equal(20, TribeWorkSpeedMath.GatherRemaining(20, 2, 3, 4, 0.25f, 1));
        Assert.Equal(0, TribeWorkSpeedMath.GatherRemaining(0, 2, 3, 4, 0.25f, 5));
    }

    [Fact]
    public void InvalidAndOverflowingWorkFailsBeforeAnyNativeWrite()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TribeWorkSpeedMath.ScaleCoefficient(1, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => TribeWorkSpeedMath.HarvestBaseDamage(float.NaN, 1, 1, 1, 1, 2));
        Assert.Throws<InvalidOperationException>(() => TribeWorkSpeedMath.HarvestBaseDamage(float.MaxValue, 0, 0, 0, 1, 5));
        Assert.Throws<InvalidOperationException>(() => TribeWorkSpeedMath.GatherRemaining(1, float.MaxValue, 0, 0, 1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => TribeWorkSpeedMath.GatherRemaining(1, 1, 1, 1, -1, 2));
    }
}
