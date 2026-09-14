using HutchASKA.Core.Compatibility;

namespace HutchASKA.Plugin.Infrastructure;

public sealed class SinglePlayerGuard
{
    // Stage 2 will populate this from verified session APIs; absence of evidence stays blocked.
    public SessionMode Mode => SessionMode.Unknown;
    public SinglePlayerDecision Decision { get; } = SinglePlayerDecision.Evaluate(SessionMode.Unknown);
}
