using System.Reflection;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;

namespace HutchASKA.Plugin.Player;

internal sealed class TemperatureImmunityFeature(IPlayerContext players)
    : TemperatureProtectionFeature("player.temperature", "Temperature Immunity")
{
    private static TemperatureImmunityFeature? active;

    public override CompatibilityResult ProbeCompatibility()
    {
        var thermal = base.ProbeCompatibility();
        if (!thermal.IsCompatible) return thermal;
        return typeof(PlayerCharacter).GetMethod("GetPlayerSurvival", Type.EmptyTypes)?.ReturnType == typeof(PlayerSurvival)
            ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("Local player survival identity is unavailable.");
    }

    protected override bool IsTarget(CharacterSurvival survival) =>
        players.TryGetLocalPlayer(out var player) && player!.GetPlayerSurvival() == survival;

    public override bool TryEnable()
    {
        active = this;
        Install(typeof(TemperatureImmunityFeature));
        return base.TryEnable();
    }

    public static void WarmthPostfix(CharacterSurvival __instance, ref float __result) =>
        __result = active?.Protect(__instance, __result, false) ?? __result;

    public static void FrostPostfix(CharacterSurvival __instance, ref float __result) =>
        __result = active?.Protect(__instance, __result, true) ?? __result;

    public override void Disable() { active = null; base.Disable(); }
}

internal abstract class TemperatureProtectionFeature(string id, string name) : NativeFeature(id, name)
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + "." + id);
    private static MethodInfo? WarmthTarget() =>
        AccessTools.DeclaredMethod(typeof(CharacterSurvival), "GetWarmthPerSecond", new[] { typeof(float) });
    private static MethodInfo? FrostTarget() =>
        AccessTools.DeclaredMethod(typeof(CharacterSurvival), "__InitalizeAttributes_b__75_3",
            new[] { typeof(float), typeof(float), typeof(float), typeof(float) });

    public override CompatibilityResult ProbeCompatibility() =>
        WarmthTarget() is { IsStatic: false, ReturnType: var warmthReturn } && warmthReturn == typeof(float)
        && FrostTarget() is { IsStatic: false, ReturnType: var frostReturn } && frostReturn == typeof(float)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Verified warmth/frost change callbacks are unavailable in this ASKA build.");

    protected void Install(Type hookType)
    {
        harmony.Patch(WarmthTarget(), postfix: new HarmonyMethod(hookType, "WarmthPostfix"));
        harmony.Patch(FrostTarget(), postfix: new HarmonyMethod(hookType, "FrostPostfix"));
        StatusReason = "Prevents further cooling and frost accumulation; existing cold/frost still needs normal recovery.";
    }

    protected abstract bool IsTarget(CharacterSurvival survival);

    protected float Protect(CharacterSurvival survival, float original, bool frost)
    {
        // Preserve native recovery and avoid ownership work when no harmful change is proposed.
        if (float.IsFinite(original) && (frost ? original <= 0 : original >= 0)) return original;
        var result = original;
        var success = Hosted?.TryExecute(() =>
        {
            if (IsTarget(survival))
                result = frost ? TemperatureDelta.PreventFrost(original) : TemperatureDelta.PreserveWarmth(original);
        }) == true;
        return success ? result : original;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        StatusReason = null;
        base.Disable();
    }
}
