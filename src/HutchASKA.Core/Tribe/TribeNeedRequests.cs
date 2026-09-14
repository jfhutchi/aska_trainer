namespace HutchASKA.Core.Tribe;

public static class TribeNeedRequests
{
    public static VillagerEditRequest RestoreAll { get; } = new(FoodFraction: 1, WaterFraction: 1,
        EnergyFraction: 1, RestFraction: 1, HappinessFraction: 1);
}
