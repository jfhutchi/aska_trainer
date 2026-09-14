namespace HutchASKA.Core.Compatibility;

/// <summary>Contains callback faults before native callers swallow them; a fault requires restart.</summary>
public sealed class CallbackGuard(Action<Exception> reportError)
{
    private readonly FeatureCircuitBreaker breaker = new(1);
    private bool reported;

    public bool IsFaulted => breaker.IsOpen;

    public bool TryRun(Action callback)
    {
        if (IsFaulted) return false;
        var succeeded = new FeatureExecutionGuard(breaker).TryRun(callback);
        if (!succeeded && !reported)
        {
            reported = true;
            reportError(breaker.LastError!);
        }
        // A nested managed delegate may fail even though its native caller returns normally.
        return succeeded && !IsFaulted;
    }
}
