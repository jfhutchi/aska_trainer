using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.World;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class WorldTab(FeatureControls controls, GameSpeedFeature speed, TimeStepFeature timeStep, WeatherOverrideFeature weather, RuntimeConfiguration config)
{
    private string? timeMessage;
    private string? weatherMessage;
    public void Clear() { timeMessage = null; weatherMessage = null; }
    internal void Draw()
    {
        DrawWeather();
        controls.Toggle("world.freeze");
        controls.Toggle("world.fuel");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(controls.Get("world.hour"));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-1 Hour"))
            timeMessage = timeStep.TryAdjust(-1, out var error) ? "Clock moved back one hour." : error;
        if (GUILayout.Button("+1 Hour"))
            timeMessage = timeStep.TryAdjust(1, out var error) ? "Clock moved forward one hour." : error;
        GUILayout.EndHorizontal();
        GUI.enabled = previous;
        if (controls.Get("world.hour").StatusReason is { } timeReason) GUILayout.Label(timeReason);
        if (timeMessage is not null) GUILayout.Label(timeMessage);
        GUILayout.Label("Changes the world clock; does not simulate an hour of work. Moving backward across midnight is unavailable.");
        var feature = controls.Get("world.speed");
        GUILayout.Label($"Game Speed: {speed.Multiplier.Value:0.0}x [{feature.State}]");
        GUI.enabled = previous && controls.CanChange(feature);
        GUILayout.BeginHorizontal();
        foreach (var multiplier in new[] { .5f, 1f, 2f, 5f })
            if (GUILayout.Button($"{multiplier:0.0}x"))
            {
                speed.Multiplier.Value = multiplier;
                config.GameSpeed.Value = multiplier;
                controls.Set(feature, multiplier != 1);
            }
        GUILayout.EndHorizontal();
        GUI.enabled = previous;
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
    }

    private void DrawWeather()
    {
        var feature = controls.Get("world.weather");
        GUILayout.Label($"Weather: {WeatherOverrideFeature.Label(weather.Choice)} [{feature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(feature);
        try
        {
            for (var row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                try
                {
                    for (var column = 0; column < 3; column++)
                    {
                        var choice = (WeatherChoice)(row * 3 + column);
                        if (GUILayout.Button(WeatherOverrideFeature.Label(choice)))
                            weatherMessage = weather.TrySelect(choice, out var error)
                                ? $"{WeatherOverrideFeature.Label(choice)} selected." : error;
                    }
                }
                finally { GUILayout.EndHorizontal(); }
            }
        }
        finally { GUI.enabled = previous; }
        if (weatherMessage is not null) GUILayout.Label(weatherMessage);
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
        GUILayout.Label("Session only. Rain and snow have normal world effects. Normal Forecast restores automatic weather; ground wetness and snow clear naturally.");
    }
}
