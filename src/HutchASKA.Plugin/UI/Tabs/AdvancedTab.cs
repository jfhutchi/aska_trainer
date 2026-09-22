using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Infrastructure;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class AdvancedTab(RuntimeConfiguration config, DiagnosticsService diagnostics)
{
    private string? message;
    public void Clear() => message = null;
    public void Draw()
    {
        GUILayout.Label("Trainer options start off whenever a world loads. Saved presets remain inactive until enabled.");
        GUILayout.Label("Configuration: BepInEx/config/com.jfhutchi.hutchaska.cfg");
        if (GUILayout.Button("Reload Configuration")) message = diagnostics.ReloadConfiguration();
        if (GUILayout.Button("Re-scan Compatibility")) message = diagnostics.RescanCompatibility();
        if (GUILayout.Button("Reset All / Restore Native Values")) message = diagnostics.ResetAll();
        GUILayout.Label($"Trainer logging: {config.Verbosity.Value}");
        GUILayout.BeginHorizontal();
        foreach (var level in Enum.GetValues<DiagnosticVerbosity>())
            if (GUILayout.Button(level.ToString())) config.Verbosity.Value = level;
        GUILayout.EndHorizontal();
        GUILayout.Label("Errors are always logged. Verbose adds lifecycle and compatibility details.");
        GUILayout.Label("F1 God Mode | F2 Stamina | F5 Freeze Time | F8 Menu. Keys are configurable.");
        GUILayout.Label("Duplicate gameplay hotkeys are ignored; the menu key takes priority.");
        if (message is not null) GUILayout.Label(message);
    }
}
