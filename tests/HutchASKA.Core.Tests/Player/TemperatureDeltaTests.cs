using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class TemperatureDeltaTests
{
    [Fact]
    public void ColdExposureCannotUndoWarmthRecovery()
    {
        var warmth = 25f;
        foreach (var delta in new[] { -5f, -2f, 4f, -10f, 3f })
            warmth += TemperatureDelta.PreserveWarmth(delta);
        Assert.Equal(32f, warmth);
    }

    [Fact]
    public void BlizzardExposureCannotUndoNaturalThaw()
    {
        var frost = 25f;
        foreach (var delta in new[] { 5f, 2f, -4f, 10f, -3f })
            frost += TemperatureDelta.PreventFrost(delta);
        Assert.Equal(18f, frost);
    }

    [Fact]
    public void ZeroRateDoesNotInventRecovery()
    {
        Assert.Equal(0f, TemperatureDelta.PreserveWarmth(0));
        Assert.Equal(0f, TemperatureDelta.PreventFrost(0));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteNativeRatesAreReported(float delta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureDelta.PreserveWarmth(delta));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureDelta.PreventFrost(delta));
    }
}
