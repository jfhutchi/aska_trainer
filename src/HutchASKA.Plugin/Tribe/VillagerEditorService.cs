using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Tribe;

internal sealed class VillagerEditorService(ITribeContext tribe) : NativeActionFeature("tribe.editor", "Villager Editor")
{
    public string? ContextError => (tribe as ITribeContextStatus)?.LastError;
    public override CompatibilityResult ProbeCompatibility() => AskaTribeContext.ProbeCompatibility();

    public bool TryGet(string stableId, out VillagerSnapshot? snapshot, out string? error)
    {
        VillagerSnapshot? value = null;
        var success = Execute(() => (tribe.TrySnapshot(stableId, out value, out var reason), reason), out error);
        snapshot = value;
        return success;
    }

    public bool TryApply(string stableId, VillagerEditRequest request, out string? error)
    {
        var success = Execute(() => (tribe.TryApply(stableId, request, out var reason), reason), out error);
        if (!success) error += " Earlier requested fields may have applied; refresh and inspect current values before retrying.";
        return success;
    }

    public bool TryHeal(string stableId, out string? error) =>
        Execute(() => (tribe.TryHeal(stableId, out var reason), reason), out error);

    public bool TryList(out IReadOnlyList<VillagerSnapshot> snapshots, out string? error)
    {
        IReadOnlyList<VillagerSnapshot> result = Array.Empty<VillagerSnapshot>();
        var success = Execute(() => (tribe.TrySnapshotAll(out result, out var reason), reason), out error);
        snapshots = success ? result : Array.Empty<VillagerSnapshot>();
        return success;
    }

    private bool Execute(Func<(bool Success, string? Error)> action, out string? error)
    {
        (bool Success, string? Error) result = (false, null);
        var ran = RunOnce(() => result = action(), out error);
        error ??= result.Error;
        return ran && result.Success;
    }
}
