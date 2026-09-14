using HutchASKA.Core.Compatibility;
using HutchASKA.Plugin.Game;
using SSSGame.Network;

namespace HutchASKA.Plugin.Infrastructure;

public sealed class SinglePlayerGuard
{
    private int failures;
    private readonly Action<Exception>? reportError;
    public SinglePlayerGuard(Action<Exception>? reportError = null) => this.reportError = reportError;
    public SessionMode Mode { get; private set; }
    public SinglePlayerDecision Decision { get; private set; } = SinglePlayerDecision.Evaluate(SessionMode.Unknown);

    public SinglePlayerDecision Refresh()
    {
        if (failures >= 3) return Decision;
        try
        {
            Mode = ReadMode();
            Decision = SinglePlayerDecision.Evaluate(Mode);
        }
        catch (Exception error)
        {
            // Session discovery is a fail-closed boundary, including stale interop objects.
            Mode = SessionMode.Unknown;
            failures++;
            reportError?.Invoke(error);
            Decision = new(false, $"Single-player state not confirmed: {error.Message}" +
                (failures >= 3 ? " (discovery stopped; restart ASKA after correcting the error)" : ""));
        }
        return Decision;
    }

    private static SessionMode ReadMode()
    {
        var session = GameObjectResolver.FindUnique<NetworkSession>();
        if (!session || session!.IsConnecting || session.IsDisconnecting) return SessionMode.Unknown;
        var parameters = session.Parameters;
        var runner = session.runner;
        if (parameters is null) return SessionMode.Unknown;
        if (parameters.role is NetworkSession.Role.Host or NetworkSession.Role.Client or
            NetworkSession.Role.AutoHostClient or NetworkSession.Role.Server) return SessionMode.Multiplayer;
        return parameters.role == NetworkSession.Role.Singleplayer && runner && runner!.IsRunning && runner.IsSinglePlayer
            ? SessionMode.SinglePlayer : SessionMode.Unknown;
    }
}
