using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class SinglePlayerDecisionTests
{
    [Theory]
    [InlineData(SessionMode.SinglePlayer, true, null)]
    [InlineData(SessionMode.Multiplayer, false, "Multiplayer/co-op session detected")]
    [InlineData(SessionMode.Unknown, false, "Single-player state not confirmed")]
    [InlineData((SessionMode)99, false, "Single-player state not confirmed")]
    public void Evaluate_FailsClosed(SessionMode mode, bool allowed, string? reason)
    {
        var result = SinglePlayerDecision.Evaluate(mode);
        Assert.Equal(allowed, result.Allowed);
        Assert.Equal(reason, result.Reason);
    }
}
