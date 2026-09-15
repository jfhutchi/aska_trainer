using HutchASKA.Core.Features;
using HutchASKA.Core.World;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame.Weather;

namespace HutchASKA.Plugin.World;

internal sealed class TimeStepFeature(IWorldContext world) : NativeActionFeature("world.hour", "Adjust Time by One Hour")
{
    public override CompatibilityResult ProbeCompatibility() =>
        typeof(WeatherSystem).GetMethod("SetGameTime", new[] { typeof(float) }) is { IsStatic: false, ReturnType: var result }
        && result == typeof(void)
        && typeof(WeatherSystem).GetProperty("TimeOfDay") is { CanRead: true, PropertyType: var hourType } && hourType == typeof(float)
        && typeof(WeatherSystem).GetProperty("DayOfYear") is { CanRead: true, PropertyType: var dayType } && dayType == typeof(int)
        && typeof(WeatherSystem).GetProperty("_initDone") is { CanRead: true, PropertyType: var readyType } && readyType == typeof(bool)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Verified native clock adjustment API is unavailable.");

    public bool TryAdjust(int direction, out string? error)
    {
        string? failure = null;
        var adjusted = false;
        var ran = RunOnce(() =>
        {
            if (!world.TryGetWeatherSystem(out var weather) || !weather!._initDone)
            {
                failure = "The world clock is still loading or refreshing. Try again shortly.";
                return;
            }
            if (!OneHourStep.TryGetTarget(weather.TimeOfDay, direction, out var target, out failure)) return;
            var day = weather.DayOfYear;
            if (target >= 24 && day == int.MaxValue)
            {
                failure = "The native day counter cannot advance further.";
                return;
            }
            var expectedDay = target >= 24 ? day + 1 : day;
            var expectedHour = target >= 24 ? target - 24 : target;
            weather.SetGameTime(target);
            // Single-player state authority executes this native RPC locally before returning.
            if (weather.DayOfYear != expectedDay || !float.IsFinite(weather.TimeOfDay)
                || Math.Abs(weather.TimeOfDay - expectedHour) > .001f)
            {
                failure = "The native clock did not confirm the requested hour. Inspect the current time before retrying.";
                return;
            }
            adjusted = true;
        }, out error);
        error ??= failure;
        if (!ran) error += " If the clock changed before the error, inspect it before retrying.";
        return ran && adjusted;
    }
}
