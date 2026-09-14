namespace HutchASKA.Core.Tribe;

public static class VillagerSearch
{
    public static IReadOnlyList<VillagerSnapshot> Filter(IEnumerable<VillagerSnapshot> villagers, string query)
    {
        query = query.Trim();
        return villagers.Where(v => v.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || v.StableId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.DisplayName, StringComparer.Ordinal)
            .ThenBy(v => v.StableId, StringComparer.Ordinal).ToArray();
    }
}
