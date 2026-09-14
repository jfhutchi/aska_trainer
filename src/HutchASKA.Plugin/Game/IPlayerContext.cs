namespace HutchASKA.Plugin.Game;

internal interface IPlayerContext
{
    bool TryGetLocalPlayer(out SSSGame.PlayerCharacter? player);
}
