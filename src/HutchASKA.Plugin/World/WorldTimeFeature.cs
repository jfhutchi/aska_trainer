using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame.Weather;

namespace HutchASKA.Plugin.World;

internal sealed class WorldTimeFeature(IWorldContext world) : NativeFeature("world.freeze", "Freeze Time")
{
    private int? weatherIdentity;
    private bool previousRunning;
    public override CompatibilityResult ProbeCompatibility()
    {
        var property = typeof(WeatherSystem).GetProperty("TimeRunningEnabled");
        return property?.PropertyType == typeof(bool) && property.CanRead && property.CanWrite
            ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Mutable world-time running state is unavailable.");
    }
    public override void Tick()
    {
        if (!world.TryGetWeatherSystem(out var weather))
        {
            if (weatherIdentity.HasValue) throw new InvalidOperationException("World unloaded while time freeze was active.");
            return;
        }
        var identity = weather!.GetInstanceID();
        if (weatherIdentity.HasValue && weatherIdentity != identity)
            throw new InvalidOperationException("World changed while time freeze was active; re-enable in the new world.");
        if (!weatherIdentity.HasValue)
        {
            previousRunning = weather.TimeRunningEnabled;
            weatherIdentity = identity;
        }
        weather.TimeRunningEnabled = false;
    }
    public override void Disable()
    {
        var identity = weatherIdentity;
        weatherIdentity = null;
        if (identity.HasValue)
        {
            if (!world.TryGetWeatherSystem(out var weather) || weather!.GetInstanceID() != identity)
                throw new InvalidOperationException("Frozen world unloaded; stale world state cannot safely be restored.");
            weather.TimeRunningEnabled = previousRunning;
        }
        base.Disable();
    }
}

internal sealed class TimeStepFeature() : NativeFeature("world.hour", "Adjust Time by One Hour")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "SetGameTime units and day-boundary behavior are unverified; +/-1 hour is disabled.");
}
