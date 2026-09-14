using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeNeedsFeature(ITribeContext tribe, string id, string name, VillagerEditRequest request)
    : NativeFeature(id, name)
{
    private long nextPass;
    public override CompatibilityResult ProbeCompatibility() => AskaTribeContext.ProbeCompatibility();
    public override void Tick()
    {
        var now = Environment.TickCount64;
        if (now < nextPass) return;
        nextPass = now + 500;
        var ids = tribe.GetCurrentVillagerIds();
        StatusReason = (tribe as ITribeContextStatus)?.LastError;
        foreach (var id in ids)
        {
            if (tribe.TryApply(id, request, out var error)) continue;
            StatusReason = error;
            if ((tribe as ITribeContextStatus)?.HasNativeFailure == true)
                throw new InvalidOperationException(error);
            return;
        }
    }
    public override void Disable() { nextPass = 0; base.Disable(); }
}

internal sealed class TribeUnavailableFeature(string id, string name, string reason) : NativeFeature(id, name)
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(reason);
}

internal sealed class TribeRestoreFeature(ITribeContext tribe, bool heal)
    : NativeActionFeature(heal ? "tribe.heal" : "tribe.restore", heal ? "Heal Entire Tribe" : "Restore All Needs")
{
    public override CompatibilityResult ProbeCompatibility() => AskaTribeContext.ProbeCompatibility();
    internal bool TryRestore(out int count, out string? error)
    {
        var affected = 0;
        string? operationError = null;
        var success = RunOnce(() =>
        {
            var ids = tribe.GetCurrentVillagerIds();
            if ((tribe as ITribeContextStatus)?.LastError is { } unavailable) { operationError = unavailable; return; }
            foreach (var id in ids)
            {
                var applied = heal ? tribe.TryHeal(id, out operationError)
                    : tribe.TryApply(id, TribeNeedRequests.RestoreAll, out operationError);
                if (!applied)
                {
                    operationError = $"Updated {affected} villager(s) before stopping: {operationError}";
                    if ((tribe as ITribeContextStatus)?.HasNativeFailure == true) throw new InvalidOperationException(operationError);
                    return;
                }
                affected++;
            }
        }, out error);
        count = affected;
        error ??= operationError;
        if (!success) error = $"Updated {affected} villager(s) before a native failure. The current villager may be partially updated; refresh before retrying. {error}";
        return success && operationError is null;
    }
}
