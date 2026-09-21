using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class RecruitChoiceStampTests
{
    private static readonly RecruitChoiceStamp Original = new(1, 2, 3, 0, 2, 5, 42,
        new(1, 2, 3, 4, 5, 6, 7), 8, new(0, 1, 2, 3, 4));

    [Fact]
    public void CurrentUnselectedCandidateCanBeApplied()
    {
        Assert.True(Original.CanApplyTo(Original with { }, false, false, false, false));
    }

    [Fact]
    public void IdentityAppearanceAndTraitChangesInvalidatePreview()
    {
        var changed = new[]
        {
            Original with { Population = 9 }, Original with { Settlement = 9 },
            Original with { Outlet = 9 }, Original with { Slot = 1 },
            Original with { ChoiceCount = 3 }, Original with { Definition = 6 },
            Original with { Name = 43 }, Original with { PortraitCamera = 9 },
            Original with { Appearance = Original.Appearance with { HairColorBits = 9 } },
            Original with { Perks = Original.Perks with { First = 9 } }
        };
        Assert.All(changed, current => Assert.False(Original.CanApplyTo(current, false, false, false, false)));
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void SummoningOrLostSelectionInvalidatesPreview(bool pending, bool active, bool selected, bool lost)
    {
        Assert.False(Original.CanApplyTo(Original, pending, active, selected, lost));
    }

    [Theory]
    [InlineData(0, 1, 2, 3, 4, true)]
    [InlineData(0, 1, -1, -1, -1, true)]
    [InlineData(-1, -1, -1, -1, -1, false)]
    [InlineData(0, -1, 2, -1, -1, false)]
    [InlineData(0, 0, 2, 3, 4, false)]
    [InlineData(0, 1, -2, -1, -1, false)]
    public void NativePerkSlotsRejectDuplicatesInvalidIdsAndGaps(int a, int b, int c, int d, int e, bool valid)
    {
        Assert.Equal(valid, new RecruitPerksStamp(a, b, c, d, e).IsValid());
    }
}
