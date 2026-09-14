namespace HutchASKA.Core.Tribe;

public sealed record VillagerSnapshot(
    string StableId,
    string DisplayName,
    float HealthFraction,
    float FoodFraction,
    float WaterFraction,
    float WarmthFraction,
    float EnergyFraction,
    float RestFraction,
    float HappinessFraction,
    float? Age);
