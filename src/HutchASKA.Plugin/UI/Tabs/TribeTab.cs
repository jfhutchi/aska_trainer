using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Tribe;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class TribeTab(FeatureControls controls, SinglePlayerGuard guard, VillagerEditorService editor,
    TribeRestoreFeature heal, TribeRestoreFeature restore)
{
    private IReadOnlyList<VillagerSnapshot> villagers = Array.Empty<VillagerSnapshot>();
    private VillagerSnapshot? selected;
    private VillagerEditRequest edits = new();
    private string query = "";
    private string? message;
    private int page;
    public void Clear() { villagers = Array.Empty<VillagerSnapshot>(); selected = null; edits = new(); message = null; query = ""; page = 0; }

    public void Draw()
    {
        foreach (var id in new[] { "tribe.god", "tribe.food", "tribe.water", "tribe.temperature", "tribe.energy", "tribe.rest", "tribe.happiness", "tribe.aging" })
            controls.Toggle(id);
        controls.Toggle("tribe.recruitment");
        var previous = GUI.enabled;
        GUI.enabled = previous && controls.CanChange(controls.Get("tribe.heal"));
        if (GUILayout.Button("Heal Entire Tribe"))
            message = heal.TryRestore(out var count, out var error) ? $"Healed {count} villager(s)." : error;
        GUI.enabled = previous && controls.CanChange(controls.Get("tribe.restore"));
        if (GUILayout.Button("Restore All Needs"))
            message = restore.TryRestore(out var count, out var error) ? $"Restored verified needs for {count} villager(s)." : error;
        GUILayout.Label("Restore All Needs leaves warmth and remaining lifetime unchanged.");
        GUI.enabled = previous && controls.CanChange(controls.Get("tribe.editor"));
        if (GUILayout.Button("Refresh Current Tribe")) RefreshList();
        GUI.enabled = previous;
        if (!guard.Decision.Allowed) Clear();
        GUILayout.Label("Search villager name or ID");
        var nextQuery = GUILayout.TextField(query);
        if (nextQuery != query) { query = nextQuery; page = 0; selected = null; edits = new(); }
        var matches = VillagerSearch.Filter(villagers, query);
        page = Math.Clamp(page, 0, Math.Max(0, (matches.Count - 1) / 12));
        GUILayout.Label($"{matches.Count} current snapshot(s) | Page {page + 1}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous")) { page = Math.Max(0, page - 1); selected = null; edits = new(); }
        if (GUILayout.Button("Next")) { page = Math.Min(Math.Max(0, (matches.Count - 1) / 12), page + 1); selected = null; edits = new(); }
        GUILayout.EndHorizontal();
        foreach (var villager in matches.Skip(page * 12).Take(12))
            if (GUILayout.Button($"{villager.DisplayName} [{villager.StableId}]")) Load(villager.StableId);
        if (selected is { } snapshot)
        {
            GUILayout.Label($"Selected: {snapshot.DisplayName} [{snapshot.StableId}]");
            GUI.enabled = previous && controls.CanChange(controls.Get("tribe.editor"));
            edits = edits with
            {
                HealthFraction = Field("Health", snapshot.HealthFraction, edits.HealthFraction),
                FoodFraction = Field("Food", snapshot.FoodFraction, edits.FoodFraction),
                WaterFraction = Field("Water", snapshot.WaterFraction, edits.WaterFraction),
                EnergyFraction = Field("Energy", snapshot.EnergyFraction, edits.EnergyFraction),
                RestFraction = Field("Rest", snapshot.RestFraction, edits.RestFraction),
                HappinessFraction = Field("Happiness (current native cap)", snapshot.HappinessFraction, edits.HappinessFraction)
            };
            GUILayout.Label($"Warmth: {snapshot.WarmthFraction:P0}. {AskaTribeContext.WarmthUnavailable}");
            GUILayout.Label(AskaTribeContext.AgeUnavailable);
            if (GUILayout.Button("Refresh Selected Villager")) Load(snapshot.StableId);
            if (GUILayout.Button("Heal")) FinishEdit(snapshot.StableId, editor.TryHeal(snapshot.StableId, out var error), error);
            if (GUILayout.Button("Max Needs")) FinishEdit(snapshot.StableId, editor.TryApply(snapshot.StableId, TribeNeedRequests.RestoreAll, out var error), error);
            if (GUILayout.Button("Apply Changes")) FinishEdit(snapshot.StableId, editor.TryApply(snapshot.StableId, edits, out var error), error);
            GUI.enabled = previous;
        }
        foreach (var id in new[] { "tribe.heal", "tribe.restore", "tribe.editor" })
            if (controls.Get(id).StatusReason is { } reason) GUILayout.Label(reason);
        if (editor.ContextError is { } contextError) GUILayout.Label(contextError);
        if (message is not null) GUILayout.Label(message);
    }

    private void RefreshList()
    {
        editor.TryList(out villagers, out message);
        selected = null; edits = new(); page = 0;
    }
    private void Load(string id)
    {
        message = null;
        if (!editor.TryGet(id, out selected, out var error))
        {
            RefreshList();
            message = error;
        }
        edits = new();
    }
    private void FinishEdit(string id, bool success, string? error)
    {
        if (success) { Load(id); message ??= "Villager updated."; }
        else { RefreshList(); message = error; }
    }
    private static float? Field(string name, float snapshot, float? edit)
    {
        var current = edit ?? snapshot;
        GUILayout.Label($"{name}: {current:P0}");
        var value = GUILayout.HorizontalSlider(current, 0, 1);
        return value != current ? value : edit;
    }
}
