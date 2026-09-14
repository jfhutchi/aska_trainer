namespace HutchASKA.Plugin.Tribe.Patches;

internal static class VillagerDamagePatch
{
    internal static VillagerGodModeFeature? Feature { get; set; }
    public static bool Prefix(object __instance) => Feature?.SuppressDamage(__instance) != true;
}
