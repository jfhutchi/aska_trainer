using SSSGame;

namespace HutchASKA.Plugin.Game;

internal sealed class AskaPlayerContext : IPlayerContext
{
    public bool TryGetLocalPlayer(out PlayerCharacter? player)
    {
        player = GameObjectResolver.FindUnique<PlayerManager>()?.LocalPlayer?.playerCharacter;
        if (player) return true;
        player = null;
        return false;
    }
}
