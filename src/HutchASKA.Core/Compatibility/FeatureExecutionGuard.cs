namespace HutchASKA.Core.Compatibility;

public sealed class FeatureExecutionGuard(FeatureCircuitBreaker breaker)
{
    public bool TryRun(Action action)
    {
        if (breaker.IsOpen) return false;
        try
        {
            action();
            return true;
        }
        catch (Exception error)
        {
            // This is the feature isolation boundary; callers surface LastError.
            breaker.RecordFailure(error);
            return false;
        }
    }
}
