using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Items;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;
using SSSGame.Combat;

namespace HutchASKA.Plugin.Items;

internal sealed class InfiniteDurabilityFeature(IPlayerContext players) : NativeFeature("items.durability", "Infinite Durability (carried equipment)")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".durability");
    private static System.Reflection.MethodInfo? DamageTarget => AccessTools.DeclaredMethod(typeof(Character),
        "_DealDurabilityDamage", new[] { typeof(EquipmentItem), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() });

    public override CompatibilityResult ProbeCompatibility()
    {
        var decay = ItemDecayPatch.Probe();
        if (!decay.IsCompatible) return decay;
        return DamageTarget?.ReturnType == typeof(void)
            && AccessTools.DeclaredMethod(typeof(DiggingMeleeObject), "_DealDurabilityDamage", Type.EmptyTypes)?.ReturnType == typeof(void)
            ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("Native equipment wear or digging wear method is missing.");
    }

    public override bool TryEnable()
    {
        ItemDecayPatch.Durability = this;
        ItemDecayPatch.EnsureInstalled();
        // The current native build shares this exact body with creature and structure hit receivers.
        harmony.Patch(DamageTarget,
            prefix: new HarmonyMethod(typeof(EquipmentWearPatch), nameof(EquipmentWearPatch.Prefix)),
            postfix: new HarmonyMethod(typeof(EquipmentWearPatch), nameof(EquipmentWearPatch.Postfix)),
            finalizer: new HarmonyMethod(typeof(EquipmentWearPatch), nameof(EquipmentWearPatch.Finalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(DiggingMeleeObject), "_DealDurabilityDamage", Type.EmptyTypes),
            prefix: new HarmonyMethod(typeof(EquipmentWearPatch), nameof(EquipmentWearPatch.DiggingPrefix)));
        return base.TryEnable();
    }

    internal bool Protects(Item? item)
    {
        var protect = false;
        return Hosted?.TryExecute(() => protect = item is not null && item.TryCast<EquipmentItem>() is not null
            && LocalItemOwnership.IsCarriedByLocalPlayer(players, item)) == true && protect;
    }

    internal bool ProtectsDigging(DiggingMeleeObject target)
    {
        var protect = false;
        return Hosted?.TryExecute(() =>
        {
            var weapon = target.WeaponObject;
            if (weapon) protect = LocalItemOwnership.IsCarriedByLocalPlayer(players, weapon.WeaponizedItem);
        }) == true && protect;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        ItemDecayPatch.Durability = null;
        ItemDecayPatch.RemoveIfUnused();
        base.Disable();
    }
}

internal static class EquipmentWearPatch
{
    public static void Prefix(EquipmentItem __0, ref float __1, out TemporaryFloatOverride __state)
    {
        __state = default;
        if (ItemDecayPatch.Durability?.Protects(__0) == true)
            __state = TemporaryFloatOverride.ZeroPositive(ref __1);
    }

    public static void Postfix(ref float __1, TemporaryFloatOverride __state) => __state.Restore(ref __1);
    public static Exception? Finalizer(Exception? __exception, ref float __1, TemporaryFloatOverride __state)
    {
        __state.Restore(ref __1);
        return __exception;
    }

    public static bool DiggingPrefix(DiggingMeleeObject __instance) =>
        ItemDecayPatch.Durability?.ProtectsDigging(__instance) != true;
}
