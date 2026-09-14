using SSSGame;

namespace HutchASKA.Plugin.Player.Patches;

internal static class PlayerDamagePatch
{
    internal static GodModeFeature? Feature { get; set; }
    public static bool Prefix(PlayerCharacter __instance) => Feature?.SuppressDamage(__instance) != true;
}
