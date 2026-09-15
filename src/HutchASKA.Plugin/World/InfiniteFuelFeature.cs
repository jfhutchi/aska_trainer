using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Attributes;
using SSSGame;

namespace HutchASKA.Plugin.World;

internal sealed class InfiniteFuelFeature() : NativeFeature("world.fuel", "Infinite Fuel (campfires & standing torches)")
{
    private const string ConsumptionMethod = "__InitializeAttributes_b__93_3";
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".fuel");
    private static InfiniteFuelFeature? instance;

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(FireStructure), ConsumptionMethod,
            new[] { typeof(float), typeof(float), typeof(float), typeof(float) })?.ReturnType == typeof(float)
        && typeof(FireStructure).GetProperty("_fuelConsumptionModifier")?.PropertyType == typeof(CustomOperationVariableAtributeModifier)
        && typeof(FireStructure).GetProperty("_fuelVAttr")?.PropertyType == typeof(VariableAttribute)
        && typeof(FireStructure).GetProperty("OwnerStructure")?.PropertyType == typeof(Structure)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native placed-fire fuel consumption callback is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FireStructure), ConsumptionMethod),
            postfix: new HarmonyMethod(typeof(InfiniteFuelFeature), nameof(ConsumptionPostfix)));
        StatusReason = "Keeps existing fuel in campfires and standing torches. Add starting fuel and light normally.";
        return base.TryEnable();
    }

    private static bool IsOwnedFire(FireStructure fire)
    {
        if (fire == null || !fire.gameObject.activeInHierarchy || fire.Object == null
            || !fire.Object.IsValid || !fire.Object.HasStateAuthority
            || fire.session == null || !fire.session.isMaster
            || fire._fuelVAttr is null || fire._fuelConsumptionModifier is null) return false;
        var owner = fire.OwnerStructure;
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        return owner != null && owner.IsValid && owner.IsActive && !owner.IsDead && !owner.Dismantled
            && settlement != null && owner.Settlement == settlement;
    }

    private static void ConsumptionPostfix(FireStructure __instance, ref float __result)
    {
        if (!float.IsFinite(__result) || __result >= 0f) return;
        var preserveFuel = false;
        instance?.Hosted?.TryExecute(() => preserveFuel = IsOwnedFire(__instance));
        // This callback returns a fuel delta, not the fuel amount. Zero leaves the
        // existing volume unchanged; positive deltas and other modifiers stay native.
        if (preserveFuel) __result = 0f;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        StatusReason = null;
        base.Disable();
    }
}
