namespace HutchASKA.Core.World;

public static class MushroomRegrowthPolicy
{
    public const float AvailabilityReferenceDays = 2;

    public static bool IsMushroom(int id, string? assetName) => (id, assetName) is
        (0x01004008, "Item_Food_BiomeMushroom1") or
        (0x01004009, "Item_Food_BiomeMushroomGrey") or
        (0x0100400a, "Item_Food_BiomeMushroomYellow");

    public static float? Target(float now, float nativeDue, float frequencyDays,
        bool replenishWhenAvailable, bool hasSeasonSchedule, float multiplier)
    {
        if (!float.IsFinite(now) || now < 0 || !float.IsFinite(nativeDue)
            || !float.IsFinite(frequencyDays) || multiplier is not (2 or 4)) return null;
        var interval = frequencyDays;
        if (interval <= 0)
        {
            if (interval != -1 || nativeDue != -1 || !replenishWhenAvailable || hasSeasonSchedule) return null;
            interval = AvailabilityReferenceDays;
        }
        else if (nativeDue < 0 || nativeDue <= now) return null;
        var candidate = now + interval / multiplier;
        if (!float.IsFinite(candidate) || candidate <= now) return null;
        // A native season boundary can precede the periodic date. Never postpone it.
        if (nativeDue >= 0) candidate = Math.Min(candidate, nativeDue);
        return candidate == nativeDue ? null : candidate;
    }
}

public readonly record struct MushroomScheduleLease(long World, long Clock, long Resource, long Data,
    float NativeDue, float AppliedDue)
{
    public bool Owns(long world, long clock, long resource, long data, float currentDue, bool dirty) =>
        !dirty && World == world && Clock == clock && Resource == resource && Data == data
        && currentDue == AppliedDue;
}
