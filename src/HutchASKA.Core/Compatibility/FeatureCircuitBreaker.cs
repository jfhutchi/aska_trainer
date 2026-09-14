namespace HutchASKA.Core.Compatibility;

public sealed class FeatureCircuitBreaker
{
    private readonly int threshold;
    public FeatureCircuitBreaker(int threshold)
    {
        if (threshold < 1) throw new ArgumentOutOfRangeException(nameof(threshold));
        this.threshold = threshold;
    }
    public int FailureCount { get; private set; }
    public Exception? LastError { get; private set; }
    public bool IsOpen => FailureCount >= threshold;
    public void RecordFailure(Exception error)
    {
        LastError = error;
        if (FailureCount < int.MaxValue) FailureCount++;
    }
}
