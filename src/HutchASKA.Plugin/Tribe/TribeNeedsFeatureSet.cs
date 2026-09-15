using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeNeedsFeature(TribeNeedsCoordinator coordinator, string id, string name, VillagerEditRequest request)
    : NativeFeature(id, name)
{
    public override CompatibilityResult ProbeCompatibility() => AskaTribeContext.ProbeCompatibility();
    public override bool TryEnable()
    {
        coordinator.Enable(Id, request);
        return base.TryEnable();
    }
    public override void Tick()
    {
        coordinator.Tick();
        StatusReason = coordinator.LastError;
    }
    public override void Disable() { coordinator.Disable(Id); base.Disable(); }
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
            tribe.TryApplyAll(heal ? new(HealthFraction: 1) : TribeNeedRequests.RestoreAll, out affected, out operationError);
            if ((tribe as ITribeContextStatus)?.HasNativeFailure == true) throw new InvalidOperationException(operationError);
        }, out error);
        count = affected;
        error ??= operationError;
        if (!success) error = $"Updated {affected} villager(s) before a native failure. The current villager may be partially updated; refresh before retrying. {error}";
        return success && operationError is null;
    }
}
