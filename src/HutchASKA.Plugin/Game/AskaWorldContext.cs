using SSSGame.Weather;

namespace HutchASKA.Plugin.Game;

internal sealed class AskaWorldContext : IWorldContext
{
    public bool TryGetWeatherSystem(out WeatherSystem? weather)
    {
        weather = WeatherSystem.Instance;
        if (weather) return true;
        weather = null;
        return false;
    }
}
