using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeCraftingFeature(IPlayerContext players) : NativeFeature("crafting.free", "Ignore Crafting Materials")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".crafting");
    private static FreeCraftingFeature? instance;
    [ThreadStatic] private static CraftScope scope;
    private readonly record struct CraftScope(IntPtr Interaction, IntPtr Agent);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(CraftInteraction), "CheckOwnedRequirements", new[] { typeof(Blueprint), typeof(IInteractionAgent) })?.ReturnType == typeof(bool)
        && AccessTools.DeclaredMethod(typeof(CraftInteraction), "_OnCraftingSuccess", new[] { typeof(IInteractionAgent) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(CraftInteraction), "_CheckOwnedBlueprintManifest", new[] { typeof(ItemManifest), typeof(IInteractionAgent) })?.ReturnType == typeof(bool)
        && typeof(ItemManifest).GetMethod("Clear", Type.EmptyTypes)?.ReturnType == typeof(void)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native crafting material-manifest transaction is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CraftInteraction), "CheckOwnedRequirements"),
            prefix: new HarmonyMethod(typeof(FreeCraftingFeature), nameof(RequirementsPrefix)),
            finalizer: new HarmonyMethod(typeof(FreeCraftingFeature), nameof(ScopeFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CraftInteraction), "_OnCraftingSuccess"),
            prefix: new HarmonyMethod(typeof(FreeCraftingFeature), nameof(SuccessPrefix)),
            finalizer: new HarmonyMethod(typeof(FreeCraftingFeature), nameof(ScopeFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(CraftInteraction), "_CheckOwnedBlueprintManifest"),
            prefix: new HarmonyMethod(typeof(FreeCraftingFeature), nameof(MaterialsPrefix)));
        return base.TryEnable();
    }

    private bool IsLocalAgent(IInteractionAgent? agent)
    {
        if (agent is null || !players.TryGetLocalPlayer(out var player)) return false;
        var localAgent = agent.TryCast<PlayerInteractionAgent>();
        if (localAgent is null || localAgent.GetCharacter() != player) return false;
        var inventory = player!.Inventory;
        if (!inventory || !inventory.initialized) return false;
        var local = inventory.GetItemCollection();
        var actual = agent.GetInventory();
        return local is not null && local.HasStateOwnership && actual is not null
            && actual.HasStateOwnership && actual.Pointer == local.Pointer;
    }

    private static void BeginScope(CraftInteraction interaction, IInteractionAgent agent, out CraftScope previous)
    {
        previous = scope;
        scope = default;
        var feature = instance;
        feature?.Hosted?.TryExecute(() =>
        {
            if (interaction && feature.IsLocalAgent(agent)) scope = new(interaction.Pointer, agent.Pointer);
        });
    }

    private static void RequirementsPrefix(CraftInteraction __instance, IInteractionAgent __1, out CraftScope __state) =>
        BeginScope(__instance, __1, out __state);

    private static void SuccessPrefix(CraftInteraction __instance, IInteractionAgent __0, out CraftScope __state) =>
        BeginScope(__instance, __0, out __state);

    private static Exception? ScopeFinalizer(Exception? __exception, CraftScope __state)
    {
        scope = __state;
        return __exception;
    }

    private static void MaterialsPrefix(CraftInteraction __instance, ItemManifest __0, IInteractionAgent __1)
    {
        var feature = instance;
        if (feature is null || __instance is null || __1 is null
            || scope.Interaction != __instance.Pointer || scope.Agent != __1.Pointer) return;
        // Only these two native callers create/rebuild temporary material manifests.
        // Success validates and consumes this same manifest before creating the native product.
        scope = default;
        feature.Hosted?.TryExecute(() =>
        {
            if (__0 is not null && feature.IsLocalAgent(__1)) __0.Clear();
        });
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        scope = default;
        base.Disable();
    }
}
