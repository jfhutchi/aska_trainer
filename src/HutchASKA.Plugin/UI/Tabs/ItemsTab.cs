using HutchASKA.Core.Items;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Items;
using UnityEngine;

namespace HutchASKA.Plugin.UI.Tabs;

internal sealed class ItemsTab(FeatureControls controls, SinglePlayerGuard guard, AskaItemCatalog catalog, GiveItemFeature give)
{
    private IReadOnlyList<ItemCatalogEntry> entries = Array.Empty<ItemCatalogEntry>();
    private IReadOnlyList<ItemCatalogEntry> matches = Array.Empty<ItemCatalogEntry>();
    private string query = "", quantity = "1";
    private string? selectedId, message;
    private int page;
    public void Clear() { entries = matches = Array.Empty<ItemCatalogEntry>(); selectedId = message = null; page = 0; }
    public void Draw()
    {
        controls.Toggle("items.durability");
        controls.Toggle("items.freshness");
        controls.Toggle("items.retain");
        if (!guard.Decision.Allowed) Clear();
        var enabled = GUI.enabled;
        GUI.enabled = enabled && controls.CanChange(controls.Get("items.catalog"));
        if (GUILayout.Button("Refresh Runtime Item Catalog"))
        {
            catalog.TryRead(out entries, out message);
            matches = ItemCatalogSearch.Filter(entries, query);
            selectedId = null; page = 0;
        }
        GUI.enabled = enabled;
        GUILayout.Label("Search display or internal name");
        var nextQuery = GUILayout.TextField(query);
        if (nextQuery != query)
        {
            query = nextQuery; page = 0; selectedId = null;
            matches = ItemCatalogSearch.Filter(entries, query);
        }
        page = Math.Clamp(page, 0, Math.Max(0, (matches.Count - 1) / 20));
        GUILayout.Label($"{matches.Count} items | Page {page + 1}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous")) { page = Math.Max(0, page - 1); selectedId = null; }
        if (GUILayout.Button("Next")) { page = Math.Min(Math.Max(0, (matches.Count - 1) / 20), page + 1); selectedId = null; }
        GUILayout.EndHorizontal();
        foreach (var entry in matches.Skip(page * 20).Take(20))
            if (GUILayout.Toggle(selectedId == entry.Id, $"{entry.DisplayName} [{entry.Id}]")) selectedId = entry.Id;
        var selected = entries.FirstOrDefault(entry => entry.Id == selectedId);
        GUILayout.Label(selected is null ? "Select an item to give." : $"Selected: {selected.DisplayName} [{selected.Id}]");
        GUILayout.Label($"Quantity (1-{InventoryService.MaximumQuantity})");
        quantity = GUILayout.TextField(quantity, 4);
        GUI.enabled = enabled && selected is not null && controls.CanChange(controls.Get("items.give"));
        if (GUILayout.Button("Give Item"))
        {
            if (!int.TryParse(quantity, out var count)) message = "Enter a whole-number quantity.";
            else if (give.TryGive(selectedId!, count, false, out message)) message = $"Added {count} item(s).";
        }
        if (GUILayout.Button("Give Stack") && give.TryGive(selectedId!, 1, true, out message)) message = "Added one native stack.";
        GUI.enabled = enabled;
        if (message is not null) GUILayout.Label(message);
        foreach (var id in new[] { "items.catalog", "items.give" })
            if (controls.Get(id).StatusReason is { } reason) GUILayout.Label(reason);
        GUILayout.Label("Item initialization and save persistence still require gameplay acceptance.");
    }
}
