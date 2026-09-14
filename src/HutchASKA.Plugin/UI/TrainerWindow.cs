using BepInEx.Configuration;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using HutchASKA.Plugin.UI.Tabs;
using HutchASKA.Plugin.Items;

namespace HutchASKA.Plugin.UI;

internal sealed class TrainerWindow
{
    private readonly SinglePlayerGuard guard;
    private readonly DiagnosticsPanel diagnostics;
    private readonly ConfigEntry<bool> restoreStates;
    private readonly RuntimeConfiguration config;
    private readonly PlayerTab playerTab;
    private readonly WorldTab worldTab;
    private readonly ItemsTab itemsTab;
    private readonly CraftingTab craftingTab;
    private readonly GUI.WindowFunction drawContents;
    private readonly Il2CppStringArray tabs = new(new[]
        { "Player", "Items", "Crafting & Building", "World", "Tribe", "Advanced", "Diagnostics" });
    private Rect bounds = new(40, 40, 850, 480);
    private Vector2 scroll;
    private int selectedTab;
    private bool open;

    public TrainerWindow(FeatureHost host, SinglePlayerGuard guard, RuntimeVersions versions, RuntimeConfiguration config,
        MovementSpeedFeature movement, GameSpeedFeature speed, AskaItemCatalog catalog, GiveItemFeature give)
    {
        this.guard = guard;
        this.config = config;
        restoreStates = config.RestoreStates;
        var controls = new FeatureControls(host, guard, config);
        playerTab = new PlayerTab(controls, movement, config);
        worldTab = new WorldTab(controls, speed, config);
        itemsTab = new ItemsTab(controls, guard, catalog, give);
        craftingTab = new CraftingTab(controls);
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

    public void UpdateContext()
    {
        if (!guard.Decision.Allowed) itemsTab.Clear();
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
                else if (selectedTab == 0) playerTab.Draw();
                else if (selectedTab == 1) itemsTab.Draw();
                else if (selectedTab == 2) craftingTab.Draw();
                else if (selectedTab == 3) worldTab.Draw();
                else if (selectedTab == 5)
                {
                    restoreStates.Value = GUILayout.Toggle(restoreStates.Value, "Restore enabled states on next launch (opt in)");
                    GUILayout.Label("Configuration is stored in BepInEx/config/com.jfhutchi.hutchaska.cfg.");
                    if (GUILayout.Button("Reset All / Restore Native Values")) config.ResetAll();
                    GUILayout.Label("F1 God Mode | F2 Stamina | F5 Freeze Time | F8 Menu. Keys are configurable.");
                    GUILayout.Label("Duplicate gameplay hotkeys are ignored; the menu key takes priority.");
                }
                else GUILayout.Label("This module is planned for a later stage.");
            }
            finally { GUILayout.EndScrollView(); }
            if (GUILayout.Button("Close")) open = false;
        }
        finally { GUILayout.EndVertical(); }
        GUI.DragWindow(new Rect(0, 0, bounds.width, 20));
    }
}
