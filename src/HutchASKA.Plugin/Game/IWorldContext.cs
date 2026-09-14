namespace HutchASKA.Plugin.Game;

internal interface IWorldContext
{
    bool TryGetWeatherSystem(out SSSGame.Weather.WeatherSystem? weather);
}
