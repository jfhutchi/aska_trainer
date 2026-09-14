namespace HutchASKA.Core.Compatibility;

public sealed record SinglePlayerDecision(bool Allowed, string? Reason)
{
    public static SinglePlayerDecision Evaluate(SessionMode mode) => mode switch
    {
        SessionMode.SinglePlayer => new(true, null),
        SessionMode.Multiplayer => new(false, "Multiplayer/co-op session detected"),
        _ => new(false, "Single-player state not confirmed")
    };
}
