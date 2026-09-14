using HutchASKA.Core.Tribe;

namespace HutchASKA.Plugin.Tribe;

internal interface ITribeContext
{
    IReadOnlyList<string> GetCurrentVillagerIds();
    bool TrySnapshot(string stableId, out VillagerSnapshot? snapshot, out string? error);
    bool TryApply(string stableId, VillagerEditRequest request, out string? error);
    bool TryHeal(string stableId, out string? error);
    bool IsCurrentVillager(object candidate);
}
