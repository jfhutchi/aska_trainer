using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class CallbackGuardTests
{
    [Fact]
    public void CallbackFailure_IsContainedAndReportedOnlyOnce()
    {
        var errors = new List<Exception>();
        var guard = new CallbackGuard(errors.Add);
        var error = new NotSupportedException("Method unstripping failed");
        Assert.False(guard.TryRun(() => throw error));
        Assert.True(guard.IsFaulted);
        var executed = false;
        for (var i = 0; i < 100; i++) Assert.False(guard.TryRun(() => executed = true));
        Assert.False(executed);
        Assert.Same(error, Assert.Single(errors));
    }

    [Fact]
    public void NativeCallbackFailure_MakesOuterRenderFailEvenWhenNativeCallerReturnsNormally()
    {
        var errors = new List<Exception>();
        var guard = new CallbackGuard(errors.Add);
        var returnedFromNativeCall = false;
        Assert.False(guard.TryRun(() =>
        {
            // The native window returns normally after calling the guarded managed delegate.
            guard.TryRun(() => throw new NotSupportedException("unstripped callback"));
            returnedFromNativeCall = true;
        }));
        Assert.True(returnedFromNativeCall);
        Assert.Single(errors);
    }

    [Fact]
    public void HealthyCallbacks_CanRenderRepeatedly()
    {
        var guard = new CallbackGuard(_ => Assert.Fail("Unexpected fault"));
        var count = 0;
        for (var i = 0; i < 100; i++) Assert.True(guard.TryRun(() => count++));
        Assert.Equal(100, count);
        Assert.False(guard.IsFaulted);
    }
}
