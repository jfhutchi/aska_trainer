using HutchASKA.Core.Features;
using UnityEngine;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class FeatureControls(FeatureHost host, SinglePlayerGuard guard, RuntimeConfiguration config)
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
        config.Remember(feature);
    }
}
