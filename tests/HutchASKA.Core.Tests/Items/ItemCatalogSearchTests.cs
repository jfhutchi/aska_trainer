using HutchASKA.Core.Items;

namespace HutchASKA.Core.Tests.Items;

public class ItemCatalogSearchTests
{
    private static readonly ItemCatalogEntry[] Items = {
        new("2", "Wood", "resource_log"), new("3", "Apple", "food_apple"), new("1", "Wood", null) };
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyQuerySortsByNameThenId(string? query) =>
        Assert.Equal(new[] { "3", "1", "2" }, ItemCatalogSearch.Filter(Items, query).Select(x => x.Id));
    [Fact]
    public void MatchesDisplayNameIgnoringCase() => Assert.Equal(2, ItemCatalogSearch.Filter(Items, "WOOD").Count);
    [Fact]
    public void MatchesInternalName() => Assert.Equal("2", Assert.Single(ItemCatalogSearch.Filter(Items, "LOG")).Id);
    [Fact]
    public void MissingQueryReturnsEmptyWithoutMutatingSource()
    {
        Assert.Empty(ItemCatalogSearch.Filter(Items, "stone"));
        Assert.Equal("2", Items[0].Id);
    }
}
