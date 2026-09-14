namespace HutchASKA.Core.Tribe;

public sealed record VillagerEditRequest(
    float? HealthFraction = null,
    float? FoodFraction = null,
    float? WaterFraction = null,
    float? WarmthFraction = null,
    float? EnergyFraction = null,
    float? RestFraction = null,
    float? HappinessFraction = null,
    float? Age = null)
{
    public bool IsEmpty => HealthFraction is null && FoodFraction is null && WaterFraction is null
        && WarmthFraction is null && EnergyFraction is null && RestFraction is null && HappinessFraction is null && Age is null;

    public VillagerEditRequest Clamp()
    {
        if (Age is { } age && (!float.IsFinite(age) || age < 0)) throw new ArgumentOutOfRangeException(nameof(Age));
        return this with
        {
            HealthFraction = Fraction(HealthFraction), FoodFraction = Fraction(FoodFraction),
            WaterFraction = Fraction(WaterFraction), WarmthFraction = Fraction(WarmthFraction),
            EnergyFraction = Fraction(EnergyFraction), RestFraction = Fraction(RestFraction),
            HappinessFraction = Fraction(HappinessFraction)
        };
    }

    private static float? Fraction(float? value)
    {
        if (value is null) return null;
        if (!float.IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value), "A finite fraction is required.");
        return Math.Clamp(value.Value, 0, 1);
    }
}
