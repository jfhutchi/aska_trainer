using HutchASKA.Core.Features;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class ForagingTab(FeatureControls controls, FishingAssistFeature fishing,
    MushroomRegrowthFeature mushrooms, RuntimeConfiguration config)
{
    private static readonly int[] Presets = { 1, 2, 4 };

    public void Draw()
    {
        var fishFeature = controls.Get("player.fishing");
        GUILayout.Label($"Fishing [{fishFeature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(fishFeature);
        try
        {
            var changed = Preset("Bite Speed", fishing.BiteSpeed, value => config.FishingBiteSpeed.Value = value);
            changed |= Preset("Rare Fish Boost", fishing.RareWeight, value => config.FishingRareWeight.Value = value);
            var easyCatch = GUILayout.Toggle(fishing.EasyCatch, "Easy Catch");
            if (easyCatch != fishing.EasyCatch)
            {
                fishing.EasyCatch = easyCatch;
                config.FishingEasyCatch.Value = easyCatch;
                changed = true;
            }
            if (changed) controls.Set(fishFeature,
                fishing.BiteSpeed.Value > 1 || fishing.RareWeight.Value > 1 || fishing.EasyCatch);
        }
        finally { GUI.enabled = previous; }
        GUILayout.Label("Cast and reel in normally. Easy Catch gives you longer to react and improves a valid catch.");
        GUILayout.Label("Rare Fish Boost changes eligible fish selection. It does not guarantee a rare fish on every cast.");
        if (fishFeature.StatusReason is { } fishingReason) GUILayout.Label(fishingReason);

        var mushroomFeature = controls.Get("world.mushrooms");
        GUILayout.Space(12);
        GUILayout.Label($"Mushrooms [{mushroomFeature.State}]");
        GUILayout.Label($"Regrowth Speed: {mushrooms.Multiplier.Value:0}x / Normal (unavailable)");
        if (mushroomFeature.StatusReason is { } mushroomReason) GUILayout.Label(mushroomReason);
    }

    private static bool Preset(string label, MultiplierSetting setting, Action<int> remember)
    {
        var changed = false;
        GUILayout.Label($"{label}: {setting.Value:0}x");
        GUILayout.BeginHorizontal();
        try
        {
            foreach (var value in Presets)
            {
                if (!GUILayout.Button(value == 1 ? "1x / Normal" : $"{value}x")) continue;
                setting.Value = value;
                remember(value);
                changed = true;
            }
        }
        finally { GUILayout.EndHorizontal(); }
        return changed;
    }
}
