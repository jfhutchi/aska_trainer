using HutchASKA.Core.Compatibility;
using HutchASKA.Plugin.Game;
using SSSGame.Network;

namespace HutchASKA.Plugin.Infrastructure;

public sealed class SinglePlayerGuard
{
    public SessionMode Mode { get; private set; }
    public SinglePlayerDecision Decision { get; private set; } = SinglePlayerDecision.Evaluate(SessionMode.Unknown);

    public SinglePlayerDecision Refresh()
    {
        try
        {
            Mode = ReadMode();
            Decision = SinglePlayerDecision.Evaluate(Mode);
        }
        catch (Exception error)
        {
            // Session discovery is a fail-closed boundary, including stale interop objects.
            Mode = SessionMode.Unknown;
            Decision = new(false, $"Single-player state not confirmed: {error.Message}");
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
