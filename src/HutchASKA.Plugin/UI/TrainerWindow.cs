using BepInEx.Configuration;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HutchASKA.Plugin.UI;

public sealed class TrainerWindow
{
    private readonly SinglePlayerGuard guard;
    private readonly DiagnosticsPanel diagnostics;
    private readonly ConfigEntry<bool> restoreStates;
    private readonly GUI.WindowFunction drawContents;
    private readonly Il2CppStringArray tabs = new(new[]
        { "Player", "Items", "Crafting & Building", "World", "Tribe", "Advanced", "Diagnostics" });
    private Rect bounds = new(40, 40, 850, 480);
    private Vector2 scroll;
    private int selectedTab;
    private bool open;

    public TrainerWindow(FeatureHost host, SinglePlayerGuard guard, RuntimeVersions versions, ConfigEntry<bool> restoreStates)
    {
        this.guard = guard;
        this.restoreStates = restoreStates;
        diagnostics = new DiagnosticsPanel(host, guard, versions);
        drawContents = (Action<int>)DrawContents;
    }

    public bool Draw()
    {
        open = true;
        bounds.width = Mathf.Min(850, Screen.width - 20);
        bounds.height = Mathf.Min(480, Screen.height - 20);
        bounds.x = Mathf.Clamp(bounds.x, 0, Mathf.Max(0, Screen.width - bounds.width));
        bounds.y = Mathf.Clamp(bounds.y, 0, Mathf.Max(0, Screen.height - bounds.height));
        bounds = GUI.Window(0x4841534B, bounds, drawContents, $"HutchASKA {Plugin.PluginVersion}");
        return open;
    }

    private void DrawContents(int id)
    {
        GUILayout.BeginVertical();
        try
        {
            GUILayout.Label(guard.Decision.Allowed ? "Single-player confirmed" : $"Blocked: {guard.Decision.Reason}");
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            scroll = GUILayout.BeginScrollView(scroll);
            try
            {
                if (selectedTab == 6) diagnostics.Draw();
                else if (selectedTab == 5)
                {
                    GUILayout.Label($"Restore enabled states on launch: {restoreStates.Value}");
                    GUILayout.Label("Configuration is stored in BepInEx/config/com.jfhutchi.hutchaska.cfg.");
                    GUILayout.Label("Stage 1 provides the trainer shell. Gameplay controls arrive in later stages.");
                }
                else GUILayout.Label("No gameplay features registered yet. All gameplay changes are off.");
            }
            finally { GUILayout.EndScrollView(); }
            if (GUILayout.Button("Close")) open = false;
        }
        finally { GUILayout.EndVertical(); }
        GUI.DragWindow(new Rect(0, 0, bounds.width, 20));
    }
}
