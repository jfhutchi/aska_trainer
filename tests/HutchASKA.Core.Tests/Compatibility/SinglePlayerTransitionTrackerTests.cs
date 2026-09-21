using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class SinglePlayerTransitionTrackerTests
{
    [Fact]
    public void ReportsFirstConfirmationThenLossAndReturn()
    {
        var tracker = new SinglePlayerTransitionTracker();

        Assert.Equal(SinglePlayerTransition.None, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.FirstConfirmed, tracker.Observe(true));
        Assert.Equal(SinglePlayerTransition.None, tracker.Observe(true));
        Assert.Equal(SinglePlayerTransition.LeftConfirmed, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.None, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.ReturnedConfirmed, tracker.Observe(true));
    }

    [Fact]
    public void StartingConfirmedStillDistinguishesAWorldReturn()
    {
        var tracker = new SinglePlayerTransitionTracker();

        Assert.Equal(SinglePlayerTransition.FirstConfirmed, tracker.Observe(true));
        Assert.Equal(SinglePlayerTransition.LeftConfirmed, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.ReturnedConfirmed, tracker.Observe(true));
    }

    [Fact]
    public void UnconfirmedStartupDoesNotReportLeavingAWorld()
    {
        var tracker = new SinglePlayerTransitionTracker();

        Assert.Equal(SinglePlayerTransition.None, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.None, tracker.Observe(false));
        Assert.Equal(SinglePlayerTransition.FirstConfirmed, tracker.Observe(true));
    }
}
