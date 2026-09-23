namespace HutchASKA.Core.Crafting;

public static class BenchCraftProgress
{
    public static float TripleIncrement(float before, float after, float target)
    {
        if (!float.IsFinite(before) || !float.IsFinite(after) || !float.IsFinite(target))
            throw new ArgumentOutOfRangeException(nameof(after), "Native crafting progress must be finite.");
        if (target <= 0 || before < 0 || after <= before || after >= target || before >= target)
            return after;
        return (float)Math.Min(target, (double)before + ((double)after - before) * 3);
    }
}
