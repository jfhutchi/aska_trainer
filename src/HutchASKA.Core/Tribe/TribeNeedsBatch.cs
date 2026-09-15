namespace HutchASKA.Core.Tribe;

public sealed class TribeNeedsBatch
{
    private readonly Dictionary<string, VillagerEditRequest> enabled = new();
    private VillagerEditRequest combined = new();
    private long nextPass;
    private Exception? failure;

    public void RecordFailure(Exception error) => failure ??= error;

    public void Set(string id, VillagerEditRequest request)
    {
        enabled[id] = request.Clamp();
        Rebuild();
    }

    public void Remove(string id)
    {
        if (enabled.Remove(id)) Rebuild();
    }

    public bool TryTake(long now, out VillagerEditRequest request)
    {
        request = combined;
        if (failure is not null)
            throw new InvalidOperationException("Shared tribe needs update failed: " + failure.Message, failure);
        if (combined.IsEmpty || now < nextPass) return false;
        nextPass = now + 500;
        return true;
    }

    private void Rebuild()
    {
        combined = new();
        foreach (var request in enabled.Values)
            combined = new(
                FoodFraction: Max(combined.FoodFraction, request.FoodFraction),
                WaterFraction: Max(combined.WaterFraction, request.WaterFraction),
                EnergyFraction: Max(combined.EnergyFraction, request.EnergyFraction),
                RestFraction: Max(combined.RestFraction, request.RestFraction),
                HappinessFraction: Max(combined.HappinessFraction, request.HappinessFraction));
        nextPass = 0;
        if (enabled.Count == 0) failure = null;
    }

    private static float? Max(float? left, float? right) =>
        left is null ? right : right is null ? left : Math.Max(left.Value, right.Value);
}
