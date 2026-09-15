using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Items;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;

namespace HutchASKA.Plugin.Items;

internal sealed class NoSpoilageFeature(IPlayerContext players) : NativeFeature("items.freshness", "No Spoilage (carried items)")
{
    public override CompatibilityResult ProbeCompatibility() => ItemDecayPatch.Probe();

    public override bool TryEnable()
    {
        ItemDecayPatch.Freshness = this;
        ItemDecayPatch.EnsureInstalled();
        return base.TryEnable();
    }

    internal bool Protects(Item? item)
    {
        var protect = false;
        return Hosted?.TryExecute(() => protect = item is not null && item.TryCast<EquipmentItem>() is null
            && LocalItemOwnership.IsCarriedByLocalPlayer(players, item)) == true && protect;
    }

    public override void Disable()
    {
        ItemDecayPatch.Freshness = null;
        ItemDecayPatch.RemoveIfUnused();
        base.Disable();
    }
}

internal static class ItemDecayPatch
{
    internal static NoSpoilageFeature? Freshness { get; set; }
    internal static InfiniteDurabilityFeature? Durability { get; set; }
    private static readonly Harmony Harmony = new(Plugin.PluginGuid + ".item-decay");
    private static bool installed;
    private static System.Reflection.MethodInfo? Target => AccessTools.DeclaredMethod(typeof(ItemDurablilityProcess),
        "Run", new[] { typeof(Item), typeof(float).MakeByRefType() });

    internal static CompatibilityResult Probe() => Target?.ReturnType == typeof(void)
        && typeof(ItemContainer).GetProperty("Inventory")?.PropertyType == typeof(ItemCollection)
        && typeof(Item).GetMethod("HasAuthority", Type.EmptyTypes)?.ReturnType == typeof(bool)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native item decay interval or current inventory ownership API is missing.");

    internal static void EnsureInstalled()
    {
        if (installed) return;
        Harmony.Patch(Target,
            prefix: new HarmonyMethod(typeof(ItemDecayPatch), nameof(Prefix)),
            postfix: new HarmonyMethod(typeof(ItemDecayPatch), nameof(Postfix)),
            finalizer: new HarmonyMethod(typeof(ItemDecayPatch), nameof(Finalizer)));
        installed = true;
    }

    internal static void RemoveIfUnused()
    {
        if (Freshness is not null || Durability is not null) return;
        Harmony.UnpatchSelf();
        installed = false;
    }

    public static void Prefix(Item __0, ref float __1, out TemporaryFloatOverride __state)
    {
        __state = default;
        if (Durability?.Protects(__0) == true || Freshness?.Protects(__0) == true)
            __state = TemporaryFloatOverride.ZeroPositive(ref __1);
    }

    public static void Postfix(ref float __1, TemporaryFloatOverride __state) => __state.Restore(ref __1);
    public static Exception? Finalizer(Exception? __exception, ref float __1, TemporaryFloatOverride __state)
    {
        __state.Restore(ref __1);
        return __exception;
    }
}
