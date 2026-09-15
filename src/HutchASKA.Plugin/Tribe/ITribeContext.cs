using HutchASKA.Core.Tribe;

namespace HutchASKA.Plugin.Tribe;

internal interface ITribeContext
{
    IReadOnlyList<string> GetCurrentVillagerIds();
    bool TrySnapshot(string stableId, out VillagerSnapshot? snapshot, out string? error);
    bool TryApply(string stableId, VillagerEditRequest request, out string? error);
    bool TryHeal(string stableId, out string? error);
    bool IsCurrentVillager(object candidate);
    bool TryApplyAll(VillagerEditRequest request, out int count, out string? error);
    bool TrySnapshotAll(out IReadOnlyList<VillagerSnapshot> snapshots, out string? error);
}
