using HutchASKA.Core.Features;
using HutchASKA.Plugin.Player;
using SSSGame;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeTemperatureFeature(ITribeContext tribe)
    : TemperatureProtectionFeature("tribe.temperature", "Temperature Immunity (Tribe)")
{
    private static TribeTemperatureFeature? active;

    public override CompatibilityResult ProbeCompatibility()
    {
        var thermal = base.ProbeCompatibility();
        if (!thermal.IsCompatible) return thermal;
        return typeof(CharacterSurvival).GetProperty("_character")?.PropertyType == typeof(Character)
            ? AskaTribeContext.ProbeCompatibility()
            : CompatibilityResult.Incompatible("Survival ownership identity is unavailable.");
    }

    protected override bool IsTarget(CharacterSurvival survival)
    {
        var owner = survival._character?.TryCast<Villager>();
        return owner && tribe.IsCurrentVillager(owner!) && owner!.GetSurvival() == survival;
    }

    public override bool TryEnable()
    {
        active = this;
        Install(typeof(TribeTemperatureFeature));
        return base.TryEnable();
    }

    public static void WarmthPostfix(CharacterSurvival __instance, ref float __result) =>
        __result = active?.Protect(__instance, __result, false) ?? __result;

    public static void FrostPostfix(CharacterSurvival __instance, ref float __result) =>
        __result = active?.Protect(__instance, __result, true) ?? __result;

    public override void Disable() { active = null; base.Disable(); }
}
