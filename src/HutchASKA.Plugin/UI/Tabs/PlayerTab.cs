using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class PlayerTab(FeatureControls controls, MovementSpeedFeature movement, HarvestSpeedFeature harvesting, RuntimeConfiguration config)
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
        var harvestFeature = controls.Get("player.harvest");
        GUILayout.Label($"Harvesting Speed: {harvesting.Multiplier.Value:0}x [{harvestFeature.State}]");
        GUI.enabled = previous && controls.CanChange(harvestFeature);
        GUILayout.BeginHorizontal();
        try
        {
            for (var multiplier = 1; multiplier <= 4; multiplier++)
            {
                if (!GUILayout.Button(multiplier == 1 ? "1x / Normal" : $"{multiplier}x")) continue;
                harvesting.Multiplier.Value = multiplier;
                config.HarvestSpeed.Value = multiplier;
                controls.Set(harvestFeature, multiplier != 1);
            }
        }
        finally { GUILayout.EndHorizontal(); GUI.enabled = previous; }
        GUILayout.Label("Player gathering and tool harvesting. Gathering changes apply to the next cycle; crops and fishing are unchanged.");
        if (harvestFeature.StatusReason is { } harvestReason) GUILayout.Label(harvestReason);
    }
}
