using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class FeatureCircuitBreakerTests
{
    [Fact]
    public void Breaker_OpensAfterThreeFailuresAndStopsExecuting()
    {
        var breaker = new FeatureCircuitBreaker(3);
        var guard = new FeatureExecutionGuard(breaker);
        for (var i = 0; i < 3; i++)
            Assert.False(guard.TryRun(() => throw new InvalidOperationException("failure")));
        Assert.True(breaker.IsOpen);
        Assert.Equal(3, breaker.FailureCount);
        Assert.Equal("failure", breaker.LastError?.Message);
        var executed = false;
        Assert.False(guard.TryRun(() => executed = true));
        Assert.False(executed);
    }

    [Fact]
    public void Success_DoesNotCountAsFailure()
    {
        var breaker = new FeatureCircuitBreaker(3);
        Assert.True(new FeatureExecutionGuard(breaker).TryRun(() => { }));
        Assert.Equal(0, breaker.FailureCount);
        breaker.RecordFailure(new Exception("one"));
        breaker.RecordFailure(new Exception("two"));
        Assert.False(breaker.IsOpen);
        Assert.Equal("two", breaker.LastError?.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidThreshold_IsRejected(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeatureCircuitBreaker(value));
}
