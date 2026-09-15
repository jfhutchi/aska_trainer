using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Items;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;

namespace HutchASKA.Plugin.Items;

internal sealed class RetainItemsOnUseFeature(IPlayerContext players) : NativeFeature("items.retain", "Retain Consumables On Use")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".retain-consumables");
    private readonly IPlayerContext playersContext = players;
    private static RetainItemsOnUseFeature? instance;
    [ThreadStatic] private static ConsumableRetentionScope? useScope;

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(Consumable), "Use", new[] { typeof(GameObject) })?.ReturnType == typeof(bool)
        && AccessTools.DeclaredMethod(typeof(Consumable), "_ApplyEffect", new[] { typeof(StatusEffectManager) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(ItemContainer), "RemoveItem",
            new[] { typeof(Item), typeof(int), typeof(ItemEventContext) })?.ReturnType == typeof(bool)
        && typeof(ItemCollection).GetProperty("Owner")?.PropertyType == typeof(GameObject)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native consumable effects, inventory owner, or use-removal API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Consumable), "Use", new[] { typeof(GameObject) }),
            prefix: new HarmonyMethod(typeof(RetainItemsOnUseFeature), nameof(UsePrefix)),
            finalizer: new HarmonyMethod(typeof(RetainItemsOnUseFeature), nameof(UseFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Consumable), "_ApplyEffect", new[] { typeof(StatusEffectManager) }),
            postfix: new HarmonyMethod(typeof(RetainItemsOnUseFeature), nameof(EffectPostfix)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemContainer), "RemoveItem",
            new[] { typeof(Item), typeof(int), typeof(ItemEventContext) }),
            prefix: new HarmonyMethod(typeof(RetainItemsOnUseFeature), nameof(RemovePrefix)));
        StatusReason = "Retains carried consumables after native player-use effects; other item spending stays unchanged.";
        return base.TryEnable();
    }

    private ConsumableRetentionScope? BeginUse(Consumable item, GameObject? user)
    {
        if (!LocalItemOwnership.IsCarriedByLocalPlayer(playersContext, item)
            || !playersContext.TryGetLocalPlayer(out var player)) return null;
        var container = item.Container;
        if (container is null) return null;
        // This is the same null-user fallback that Consumable.Use resolves natively.
        var consumer = user is null ? container.Inventory?.Owner : user;
        return consumer != null && consumer == player!.gameObject
            ? new(item.Pointer.ToInt64(), container.Pointer.ToInt64()) : null;
    }

    private static void UsePrefix(Consumable __instance, GameObject? __0, out ConsumableRetentionScope? __state)
    {
        __state = useScope;
        useScope = null;
        var feature = instance;
        feature?.Hosted?.TryExecute(() => useScope = feature.BeginUse(__instance, __0));
    }

    private static Exception? UseFinalizer(Exception? __exception, ConsumableRetentionScope? __state)
    {
        useScope = __state;
        return __exception;
    }

    private static void EffectPostfix(Consumable __instance) => useScope?.EffectsApplied(__instance.Pointer.ToInt64());

    private static bool RemovePrefix(ItemContainer __instance, Item __0, int __1, ItemEventContext __2, ref bool __result)
    {
        var feature = instance;
        var scope = useScope;
        if (feature is null || scope is null) return true;
        var retain = false;
        var executed = feature.Hosted?.TryExecute(() =>
        {
            if (LocalItemOwnership.IsCarriedByLocalPlayer(feature.playersContext, __0)
                && __0.Container?.Pointer == __instance.Pointer)
                retain = scope.TryRetain(__0.Pointer.ToInt64(), __instance.Pointer.ToInt64(),
                    __1, __2 == ItemEventContext.Default);
        }) == true;
        if (!executed || !retain) return true;
        // Effects already ran. Skip the decrement and zero-stack detachment together.
        __result = true;
        return false;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        useScope = null;
        StatusReason = null;
        base.Disable();
    }
}
