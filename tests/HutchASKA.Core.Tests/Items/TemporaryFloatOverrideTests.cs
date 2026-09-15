using HutchASKA.Core.Items;

namespace HutchASKA.Core.Tests.Items;

public sealed class TemporaryFloatOverrideTests
{
    [Fact]
    public void NativeCallGetsZeroButNextProcessGetsOriginalInterval()
    {
        var interval = 2.5f;
        var state = TemporaryFloatOverride.ZeroPositive(ref interval);
        Assert.Equal(0, interval);
        state.Restore(ref interval);
        Assert.Equal(2.5f, interval);
        state.Restore(ref interval);
        Assert.Equal(2.5f, interval);
    }

    [Fact]
    public void NestedOverridesRestoreOuterThenCallerInput()
    {
        var damage = 3f;
        var outer = TemporaryFloatOverride.ZeroPositive(ref damage);
        var inner = TemporaryFloatOverride.ZeroPositive(ref damage);
        inner.Restore(ref damage);
        Assert.Equal(0, damage);
        outer.Restore(ref damage);
        Assert.Equal(3, damage);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RepairOrInvalidInputIsNotChanged(float input)
    {
        var original = input;
        var state = TemporaryFloatOverride.ZeroPositive(ref input);
        Assert.False(state.Applied);
        state.Restore(ref input);
        Assert.Equal(original, input);
    }
}
