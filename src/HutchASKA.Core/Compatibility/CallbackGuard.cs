namespace HutchASKA.Core.Compatibility;

/// <summary>Contains callback faults before native callers swallow them; a fault requires restart.</summary>
public sealed class CallbackGuard
{
    private readonly FeatureCircuitBreaker breaker = new(1);
    private readonly FeatureExecutionGuard execution;
    private readonly Action<Exception> reportError;
    private bool reported;

    public CallbackGuard(Action<Exception> reportError)
    {
        this.reportError = reportError;
        execution = new FeatureExecutionGuard(breaker);
    }

    public bool IsFaulted => breaker.IsOpen;

    public bool TryRun(Action callback)
    {
        if (IsFaulted) return false;
        var succeeded = execution.TryRun(callback);
        if (!succeeded && !reported)
        {
            reported = true;
            reportError(breaker.LastError!);
        }
        // A nested managed delegate may fail even though its native caller returns normally.
        return succeeded && !IsFaulted;
    }
}
