using HutchASKA.Core.Configuration;
using HutchASKA.Core.Input;

namespace HutchASKA.Core.Tests.Configuration;

public sealed class TrainerSettingsTests
{
    [Fact]
    public void Defaults_AreSafe()
    {
        var settings = TrainerSettings.CreateDefaults();
        Assert.False(settings.RestoreEnabledStatesOnLaunch);
        Assert.Equal(1f, settings.MovementSpeedMultiplier);
        Assert.Equal(1f, settings.GameSpeedMultiplier);
        Assert.Equal("F8", settings.MenuHotkey);
    }
    [Theory]
    [InlineData(0.1f, 1f)]
    [InlineData(3f, 3f)]
    [InlineData(10f, 5f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(float.PositiveInfinity, 1f)]
    public void Movement_IsFiniteAndBounded(float value, float expected) =>
        Assert.Equal(expected, TrainerSettings.ClampMovementMultiplier(value));

    [Fact]
    public void Hotkeys_RejectConflictsWithoutChangingMapping()
    {
        var map = HotkeyMap.CreateDefaults();
        Assert.Contains("F1", Assert.Throws<InvalidOperationException>(() => map.Set("stamina", "f1")).Message);
        Assert.Equal("F2", map.Get("stamina"));
        map.Set("stamina", "");
        map.Set("god-mode", "");
        map.Set("stamina", "f3");
        Assert.Equal("F3", map.Get("STAMINA"));
        Assert.Equal("F5", map.Get("freeze-time"));
    }
}
