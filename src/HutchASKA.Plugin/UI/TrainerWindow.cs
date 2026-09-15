using HutchASKA.Core.Compatibility;
using HutchASKA.Plugin.Infrastructure;
using UnityEngine;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using HutchASKA.Plugin.UI.Tabs;
using HutchASKA.Plugin.Items;
using HutchASKA.Plugin.Tribe;

namespace HutchASKA.Plugin.UI;

internal sealed class TrainerWindow
{
    private readonly SinglePlayerGuard guard;
    private readonly DiagnosticsTab diagnostics;
    private readonly AdvancedTab advanced;
    private readonly PlayerTab playerTab;
    private readonly WorldTab worldTab;
    private readonly ItemsTab itemsTab;
    private readonly CraftingTab craftingTab;
    private readonly TribeTab tribeTab;
    private readonly GUI.WindowFunction drawContents;
    private readonly CallbackGuard renderGuard;
    private readonly TrainerTheme theme = new();
    private readonly string[] tabs =
        { "Player", "Items", "Crafting & Building", "World", "Tribe", "Advanced", "Diagnostics" };
    private Rect bounds = new(20, 20, 960, 620);
    private Vector2 scroll;
    private int selectedTab;
    private bool closeRequested;

    public TrainerWindow(FeatureHost host, SinglePlayerGuard guard, RuntimeVersions versions, RuntimeConfiguration config,
        MovementSpeedFeature movement, GameSpeedFeature speed, TimeStepFeature timeStep, HarvestSpeedFeature harvesting, BuildSpeedFeature building, TerrainLevelingFeature terrain, AskaItemCatalog catalog, GiveItemFeature give,
        VillagerEditorService editor, TribeRestoreFeature healTribe, TribeRestoreFeature restoreTribe,
        DiagnosticsService diagnosticsService, Action<Exception> reportRenderError)
    {
        this.guard = guard;
        renderGuard = new CallbackGuard(reportRenderError);
        var controls = new FeatureControls(host, guard, config);
        playerTab = new PlayerTab(controls, movement, harvesting, config);
        worldTab = new WorldTab(controls, speed, timeStep, config);
        itemsTab = new ItemsTab(controls, guard, catalog, give);
        craftingTab = new CraftingTab(controls, building, terrain, config);
        tribeTab = new TribeTab(controls, guard, editor, healTribe, restoreTribe);
        diagnostics = new DiagnosticsTab(diagnosticsService);
        advanced = new AdvancedTab(config, diagnosticsService);
        config.TransientStateCleared += ClearTransientState;
        drawContents = (Action<int>)DrawContents;
    }

    public bool Draw()
    {
        return renderGuard.TryRun(DrawWindow) && !closeRequested;
    }

    public void Open() => closeRequested = false;

    public bool CanDraw => !renderGuard.IsFaulted;

    private void DrawWindow()
    {
        bounds.width = Mathf.Min(960, Screen.width - 20);
        bounds.height = Mathf.Min(620, Screen.height - 20);
        bounds.x = Mathf.Clamp(bounds.x, 0, Mathf.Max(0, Screen.width - bounds.width));
        bounds.y = Mathf.Clamp(bounds.y, 0, Mathf.Max(0, Screen.height - bounds.height));
        var previousSkin = GUI.skin;
        var previousColor = GUI.color;
        var previousContent = GUI.contentColor;
        var previousBackground = GUI.backgroundColor;
        var previousEnabled = GUI.enabled;
        try
        {
            theme.Initialize(previousSkin);
            GUI.skin = theme.Skin;
            // Other OnGUI callbacks can leave tint/alpha/disabled state behind. Own our draw state.
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            bounds = GUI.Window(0x4841534B, bounds, drawContents, $"HutchASKA {Plugin.PluginVersion}");
        }
        finally
        {
            GUI.skin = previousSkin;
            GUI.color = previousColor;
            GUI.contentColor = previousContent;
            GUI.backgroundColor = previousBackground;
            GUI.enabled = previousEnabled;
        }
    }

    public void ReleaseResources() => theme.Dispose();

    public void UpdateContext()
    {
        if (!guard.Decision.Allowed) { itemsTab.Clear(); tribeTab.Clear(); }
    }

    private void ClearTransientState() { itemsTab.Clear(); tribeTab.Clear(); worldTab.Clear(); advanced.Clear(); }

    private void DrawContents(int id)
    {
        // Catch inside the native delegate: the IL2CPP trampoline otherwise swallows the fault.
        renderGuard.TryRun(() =>
        {
            var previousEnabled = GUI.enabled;
            try { DrawContentsCore(); }
            finally { GUI.enabled = previousEnabled; }
        });
    }

    private void DrawContentsCore()
    {
        // Draw the footer before entering any scroll/layout group so its hit target is stable.
        GUI.enabled = true;
        if (GUI.Button(new Rect(14, bounds.height - 48, bounds.width - 28, 34), "Close"))
            closeRequested = true;
        GUILayout.BeginVertical();
        try
        {
            GUILayout.Label(guard.Decision.Allowed ? "Single-player confirmed" : $"Blocked: {guard.Decision.Reason}");
            DrawTabs();
            scroll = GUILayout.BeginScrollView(scroll);
            try
            {
                if (selectedTab == 6) diagnostics.Draw();
                else if (selectedTab == 0) playerTab.Draw();
                else if (selectedTab == 1) itemsTab.Draw();
                else if (selectedTab == 2) craftingTab.Draw();
                else if (selectedTab == 3) worldTab.Draw();
                else if (selectedTab == 4) tribeTab.Draw();
                else if (selectedTab == 5) advanced.Draw();
                else GUILayout.Label("This module is planned for a later stage.");
            }
            finally { GUILayout.EndScrollView(); }
            GUILayout.Space(42);
        }
        finally { GUILayout.EndVertical(); }
        GUI.DragWindow(new Rect(0, 0, bounds.width, 32));
    }

    private void DrawTabs()
    {
        // Both GUIContent.Temp(string[]) and the final Toolbar overload are stripped in ASKA.
        // Two rows leave room for readable labels at smaller window sizes.
        for (var row = 0; row < 2; row++)
        {
            GUILayout.BeginHorizontal();
            try
            {
                for (var i = row * 4; i < Math.Min(row * 4 + 4, tabs.Length); i++)
                {
                    var style = i == selectedTab ? theme.SelectedTab : theme.Skin.button;
                    if (GUILayout.Button(tabs[i], style) && i != selectedTab)
                    {
                        selectedTab = i;
                        scroll = Vector2.zero;
                    }
                }
            }
            finally { GUILayout.EndHorizontal(); }
        }
    }
}
