using HutchASKA.Plugin.Infrastructure;
using UnityEngine;

namespace HutchASKA.Plugin.UI;

public sealed record RuntimeVersions(string Game, string Unity, string BepInEx, string SteamBuild = "Unavailable");

public sealed class DiagnosticsPanel(FeatureHost host, SinglePlayerGuard guard, RuntimeVersions versions)
{
    public void Draw()
    {
        GUILayout.Label($"HutchASKA: {Plugin.PluginVersion}");
        GUILayout.Label($"ASKA Steam build: {versions.SteamBuild}");
        GUILayout.Label($"ASKA application version: {versions.Game}");
        GUILayout.Label($"Unity: {versions.Unity}");
        GUILayout.Label($"BepInEx: {versions.BepInEx}");
        GUILayout.Label($"Session: {guard.Mode}; allowed: {guard.Decision.Allowed}");
        if (guard.Decision.Reason is { } reason) GUILayout.Label(reason);
        var features = host.Registry.Snapshot();
        if (features.Count == 0) GUILayout.Label("No gameplay features registered.");
        foreach (var feature in features)
        {
            GUILayout.Label($"{feature.DisplayName} [{feature.Id}]: {feature.State}");
            if (feature.StatusReason is { } status) GUILayout.Label(status);
        }
        GUILayout.Label("Full errors are recorded in BepInEx/LogOutput.log.");
    }
}
