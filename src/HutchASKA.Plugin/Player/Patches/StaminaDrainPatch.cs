using SSSGame;
using SSSGame.Controllers;

namespace HutchASKA.Plugin.Player.Patches;

internal static class StaminaDrainPatch
{
    internal static InfiniteStaminaFeature? Feature { get; set; }
    public static bool CharacterPrefix(Character __instance) => Feature?.IsLocal(__instance) != true;
    public static bool MovementPrefix(CharacterMovement __instance) => Feature?.IsLocal(__instance) != true;
    public static bool TryPrefix(CharacterMovement __instance, ref bool __result)
    {
        if (Feature?.IsLocal(__instance) != true) return true;
        __result = true;
        return false;
    }
}
