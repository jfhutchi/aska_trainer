using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class PlayerTab(FeatureControls controls, MovementSpeedFeature movement, RuntimeConfiguration config)
{
    internal void Draw()
    {
        foreach (var id in new[] { "player.god", "player.stamina", "player.hunger", "player.thirst", "player.temperature" }) controls.Toggle(id);
        var feature = controls.Get("player.movement");
        GUILayout.Label($"Movement Speed: {movement.Multiplier.Value:0.0}x [{feature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(feature);
        var value = Mathf.Round(GUILayout.HorizontalSlider(movement.Multiplier.Value, 1, 5) * 10) / 10;
        if (value != movement.Multiplier.Value)
        {
            movement.Multiplier.Value = value;
            config.Movement.Value = value;
            controls.Set(feature, value != 1);
        }
        if (GUILayout.Button("Reset Movement to 1x"))
        {
            movement.Multiplier.Reset();
            config.Movement.Value = 1;
            controls.Set(feature, false);
        }
        GUI.enabled = previous;
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
    }
}
