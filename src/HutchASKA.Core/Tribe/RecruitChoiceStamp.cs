namespace HutchASKA.Core.Tribe;

public sealed record RecruitAppearanceStamp(int Hair, int FacialFeature, int HairColorBits,
    int SkinColorBits, int TattooColorBits, int Iris, int Tattoo);

public sealed record RecruitPerksStamp(int First, int Second, int Third, int Fourth, int Fifth)
{
    public int[] ToArray() => new[] { First, Second, Third, Fourth, Fifth };

    // Native PerksData uses -1 for trailing empty slots. Zero is a valid table id.
    public bool IsValid()
    {
        var seen = new HashSet<int>();
        var ended = false;
        foreach (var id in ToArray())
        {
            if (id == -1) { ended = true; continue; }
            if (id < 0 || ended || !seen.Add(id)) return false;
        }
        return seen.Count > 0;
    }
}

public sealed record RecruitChoiceStamp(int Population, int Settlement, int Outlet, int Slot,
    int ChoiceCount, int Definition, int Name, RecruitAppearanceStamp Appearance,
    int PortraitCamera, RecruitPerksStamp Perks)
{
    public bool CanApplyTo(RecruitChoiceStamp current, bool spawnPending, bool activated,
        bool selectedForSummoning, bool lostVillagerSelected) =>
        !spawnPending && !activated && !selectedForSummoning && !lostVillagerSelected
        && Slot >= 0 && ChoiceCount >= 2 && Slot < ChoiceCount && this == current;
}
