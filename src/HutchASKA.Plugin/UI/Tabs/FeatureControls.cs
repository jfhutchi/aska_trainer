using HutchASKA.Core.Features;
using UnityEngine;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class FeatureControls(FeatureHost host, SinglePlayerGuard guard)
{
    internal ITrainerFeature Get(string id) => host.Registry.Find(id) ?? throw new InvalidOperationException($"Feature {id} is not registered.");
    internal bool CanChange(ITrainerFeature feature) => guard.Decision.Allowed && feature.State is not (FeatureState.Faulted or FeatureState.Incompatible);
    internal void Toggle(string id)
    {
        var feature = Get(id);
        var previous = GUI.enabled;
        GUI.enabled = previous && CanChange(feature);
        var isEnabled = feature.State == FeatureState.Enabled;
        var next = GUILayout.Toggle(isEnabled, feature.DisplayName);
        GUI.enabled = previous;
        if (next != isEnabled) Set(feature, next);
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
    }
    internal void Set(ITrainerFeature feature, bool enabled)
    {
        if (enabled) feature.TryEnable(); else feature.Disable();
    }

    internal void MultiplierPresets(string id, MultiplierSetting setting, int maximum, Action<int> remember)
    {
        var feature = Get(id);
        GUILayout.Label($"{feature.DisplayName}: {setting.Value:0}x [{feature.State}]");
        var previous = GUI.enabled;
        GUI.enabled = previous && CanChange(feature);
        GUILayout.BeginHorizontal();
        try
        {
            for (var value = 1; value <= maximum; value++)
            {
                if (!GUILayout.Button(value == 1 ? "1x / Normal" : $"{value}x")) continue;
                setting.Value = value;
                remember(value);
                Set(feature, value > 1);
            }
        }
        finally { GUILayout.EndHorizontal(); GUI.enabled = previous; }
        if (feature.StatusReason is { } reason) GUILayout.Label(reason);
    }
}
