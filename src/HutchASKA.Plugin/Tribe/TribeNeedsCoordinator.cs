using HutchASKA.Core.Tribe;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeNeedsCoordinator(ITribeContext tribe)
{
    private readonly TribeNeedsBatch batch = new();
    public string? LastError { get; private set; }
    public void Enable(string id, VillagerEditRequest request) => batch.Set(id, request);
    public void Disable(string id) => batch.Remove(id);

    public void Tick()
    {
        try
        {
            if (!batch.TryTake(Environment.TickCount64, out var request)) return;
            tribe.TryApplyAll(request, out _, out var error);
            LastError = error;
            if ((tribe as ITribeContextStatus)?.HasNativeFailure == true)
                throw new InvalidOperationException(error);
        }
        catch (Exception error)
        {
            // Every participating feature must see a shared failure, including callers between passes.
            batch.RecordFailure(error);
            LastError = error.Message;
            throw;
        }
    }
}
