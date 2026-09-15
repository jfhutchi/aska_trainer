using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.World;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class WorldTab(FeatureControls controls, GameSpeedFeature speed, TimeStepFeature timeStep, RuntimeConfiguration config)
{
    private string? timeMessage;
    public void Clear() => timeMessage = null;
    internal void Draw()
    {
        controls.Toggle("world.freeze");
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
}
