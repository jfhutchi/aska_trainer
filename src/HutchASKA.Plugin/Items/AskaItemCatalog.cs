using System.Globalization;
using HutchASKA.Core.Features;
using HutchASKA.Core.Items;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;

namespace HutchASKA.Plugin.Items;

internal sealed class AskaItemCatalog() : NativeActionFeature("items.catalog", "Runtime Item Catalog")
{
    public override CompatibilityResult ProbeCompatibility() =>
        typeof(ItemInfoDatabase).GetProperty("CompleteItemInfoList") is not null
        && typeof(ItemInfoList).GetProperty("itemInfoList") is not null
        ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Native item definitions are unavailable.");

    public bool TryRead(out IReadOnlyList<ItemCatalogEntry> entries, out string? error)
    {
        IReadOnlyList<ItemCatalogEntry> result = Array.Empty<ItemCatalogEntry>();
        string? failure = null;
        var ran = RunOnce(() =>
        {
            var database = GameObjectResolver.FindUnique<ItemInfoDatabase>();
            if (!database || !database!.CompleteItemInfoList) { failure = "Item definitions are not loaded."; return; }
            var list = database.CompleteItemInfoList.itemInfoList;
            if (list is null) { failure = "Item definition list is unavailable."; return; }
            var metadata = new List<ItemCatalogEntry>();
            for (var i = 0; i < list.Count; i++)
            {
                var info = list[i];
                if (!info) continue;
                metadata.Add(new(info.id.ToString(CultureInfo.InvariantCulture), info.Name ?? info.name, info.name));
            }
            result = ItemCatalogSearch.Filter(metadata.DistinctBy(x => x.Id).ToArray(), null);
        }, out error);
        entries = result;
        error ??= failure;
        return ran && failure is null;
    }
}
