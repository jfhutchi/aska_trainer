using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class VillagerSearchTests
{
    private static VillagerSnapshot Villager(string id, string name) => new(id, name, 1, 1, 1, .5f, 1, 1, 1, null);
    [Theory]
    [InlineData("AL", "guid-1")]
    [InlineData("GUID-2", "guid-2")]
    public void SearchMatchesNameOrStableIdIgnoringCase(string query, string expected)
    {
        var matches = VillagerSearch.Filter(new[] { Villager("guid-2", "Bjorn"), Villager("guid-1", "Alva") }, query);
        Assert.Equal(expected, Assert.Single(matches).StableId);
    }
    [Fact]
    public void SortingHasStableTieBreakersIndependentOfPopulationOrder()
    {
        var villagers = new[] { Villager("2", "Alva"), Villager("1", "Alva"), Villager("3", "Bjorn") };
        Assert.Equal(new[] { "1", "2", "3" }, VillagerSearch.Filter(villagers.AsEnumerable().Reverse(), " ").Select(v => v.StableId));
    }
}
