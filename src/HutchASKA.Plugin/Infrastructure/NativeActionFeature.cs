namespace HutchASKA.Plugin.Infrastructure;

internal abstract class NativeActionFeature(string id, string name) : NativeFeature(id, name)
{
    protected bool RunOnce(Action action, out string? error)
    {
        var hosted = Hosted ?? throw new InvalidOperationException("Action is not registered.");
        if (!hosted.TryEnable()) { error = hosted.StatusReason ?? "Action unavailable."; return false; }
        try
        {
            var success = hosted.TryExecute(action);
            error = success ? null : hosted.StatusReason ?? "Action blocked.";
            return success;
        }
        finally { hosted.Disable(); }
    }
}
