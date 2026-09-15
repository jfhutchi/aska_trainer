using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class CraftingTab(FeatureControls controls, BuildSpeedFeature building, TerrainLevelingFeature terrain, RuntimeConfiguration config)
{
    public void Draw()
    {
        controls.Toggle("crafting.free");
        controls.Toggle("building.free");
        controls.Toggle("repairs.free");
        var feature = controls.Get("player.buildspeed");
        GUILayout.Label($"Build Speed: {building.Multiplier.Value:0}x [{feature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(feature);
        GUILayout.BeginHorizontal();
        try
        {
            for (var multiplier = 1; multiplier <= 4; multiplier++)
                if (GUILayout.Button(multiplier == 1 ? "1x / Normal" : $"{multiplier}x"))
                {
                    building.Multiplier.Value = multiplier;
                    config.BuildSpeed.Value = multiplier;
                    controls.Set(feature, multiplier != 1);
                }
        }
        finally { GUILayout.EndHorizontal(); GUI.enabled = previous; }
        GUILayout.Label("Increases construction work per hammer stroke. Material requirements are controlled by Free Building.");
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
        var terrainFeature = controls.Get("player.terrain");
        GUILayout.Label($"{terrain.DisplayName}: normal game area [{terrainFeature.State}]");
        if (terrainFeature.StatusReason is { } terrainReason) GUILayout.Label(terrainReason);
    }
}
