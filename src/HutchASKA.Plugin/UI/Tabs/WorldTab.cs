using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.World;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class WorldTab(FeatureControls controls, GameSpeedFeature speed, RuntimeConfiguration config)
{
    internal void Draw()
    {
        controls.Toggle("world.freeze");
        var previous = GUI.enabled;
        GUI.enabled = false;
        GUILayout.BeginHorizontal();
        GUILayout.Button("-1 Hour");
        GUILayout.Button("+1 Hour");
        GUILayout.EndHorizontal();
        GUI.enabled = previous;
        GUILayout.Label(controls.Get("world.hour").StatusReason);
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
