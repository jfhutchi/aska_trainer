namespace HutchASKA.Core.Items;

public static class ItemCatalogSearch
{
    public static IReadOnlyList<ItemCatalogEntry> Filter(IReadOnlyList<ItemCatalogEntry> source, string? query) =>
        source.Where(x => string.IsNullOrEmpty(query)
            || x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (x.InternalName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
        .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
}
