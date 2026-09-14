using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class DiagnosticsTab(DiagnosticsService diagnostics)
{
    public void Draw()
    {
        var versions = diagnostics.Versions;
        GUILayout.Label($"HutchASKA: {Plugin.PluginVersion}");
        GUILayout.Label($"ASKA Steam build: {versions.SteamBuild}");
        GUILayout.Label($"ASKA application version: {versions.Game}");
        GUILayout.Label($"Unity: {versions.Unity}");
        GUILayout.Label($"BepInEx: {versions.BepInEx}");
        GUILayout.Label($"Session: {diagnostics.SessionStatus}");
        if (diagnostics.TribeStatus is { } tribe) GUILayout.Label($"Tribe context: {tribe}");
        foreach (var feature in diagnostics.Features)
        {
            GUILayout.Label($"{feature.DisplayName} [{feature.Id}]: {feature.State}");
            if (feature.StatusReason is { } reason) GUILayout.Label(reason);
            if (feature is HostedFeature { HasPendingCleanup: true }) GUILayout.Label("Native cleanup pending; explicit Reset All can retry restoration.");
        }
        GUILayout.Label("Full exceptions: BepInEx/LogOutput.log. Gameplay acceptance remains pending; compilation is not a runtime pass.");
    }
}
