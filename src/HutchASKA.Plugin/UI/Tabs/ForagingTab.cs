using HutchASKA.Core.Features;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class ForagingTab(FeatureControls controls, FishingAssistFeature fishing,
    MushroomRegrowthFeature mushrooms, HarvestSpeedFeature harvesting, RuntimeConfiguration config)
{
    private static readonly int[] BitePresets = { 1, 2, 4 };
    private static readonly int[] RarePresets = { 1, 20, 30, 40, 50 };

    public void Draw()
    {
        var fishFeature = controls.Get("player.fishing");
        GUILayout.Label($"Fishing [{fishFeature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(fishFeature);
        try
        {
            var changed = Preset("Bite Speed", fishing.BiteSpeed, BitePresets, value => config.FishingBiteSpeed.Value = value);
            changed |= Preset("Rare Fish Boost", fishing.RareWeight, RarePresets, value => config.FishingRareWeight.Value = value);
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

        GUILayout.Space(12);
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

        var mushroomFeature = controls.Get("world.mushrooms");
        GUILayout.Space(12);
        GUILayout.Label($"Mushrooms [{mushroomFeature.State}]");
        GUILayout.Label($"Regrowth Speed: {mushrooms.Multiplier.Value:0}x / Normal (unavailable)");
        if (mushroomFeature.StatusReason is { } mushroomReason) GUILayout.Label(mushroomReason);
    }

    private static bool Preset(string label, MultiplierSetting setting, int[] presets, Action<int> remember)
    {
        var changed = false;
        GUILayout.Label($"{label}: {setting.Value:0}x");
        GUILayout.BeginHorizontal();
        try
        {
            foreach (var value in presets)
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
