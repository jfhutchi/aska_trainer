extern alias UnityCore;
using NativeObject = UnityCore::UnityEngine.Object;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeRepairsFeature() : NativeFeature("repairs.free", "Free Repairs")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".repairs");
    private static FreeRepairsFeature? instance;
    [ThreadStatic] private static RepairScope scope;
    private bool initialScanPending;
    private readonly record struct RepairScope(RepairPart? Part, IntPtr Container);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(RepairPart), "_RefreshPartsStatus", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(RepairPart), "_RefreshRepairsStatus", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(RepairPart), "Initialize", new[] { typeof(Structure) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(RepairPart), "OnNetworkStageChanged", new[] { typeof(RepairPart.Stage) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(ItemContainer), "GetFillRatio", new[] { typeof(bool) })?.ReturnType == typeof(float)
        && typeof(RepairPart).GetProperty("networkRepairPart")?.PropertyType == typeof(NetworkRepairPart)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native repair supply qualification is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(RepairPart), "_RefreshPartsStatus"),
            prefix: new HarmonyMethod(typeof(FreeRepairsFeature), nameof(SupplyPrefix)),
            finalizer: new HarmonyMethod(typeof(FreeRepairsFeature), nameof(SupplyFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemContainer), "GetFillRatio"),
            postfix: new HarmonyMethod(typeof(FreeRepairsFeature), nameof(FillRatioPostfix)));
        foreach (var method in new[] { "Initialize", "_RefreshRepairsStatus", "OnNetworkStageChanged" })
            harmony.Patch(AccessTools.DeclaredMethod(typeof(RepairPart), method),
                postfix: new HarmonyMethod(typeof(FreeRepairsFeature), nameof(RefreshPostfix)));
        initialScanPending = true;
        StatusReason = "Waives outstanding repair materials; deposited supplies stay committed. Repair work is still required.";
        return base.TryEnable();
    }

    private static bool IsOwnedSupply(RepairPart? part)
    {
        if (part == null || !part.gameObject.activeInHierarchy || !part._initialized) return false;
        var network = part.networkRepairPart;
        if (network == null || network.Object == null || !network.Object.IsValid
            || !network.Object.HasStateAuthority || network.session == null || !network.session.isMaster
            || part.CurrentStage != RepairPart.Stage.Supply) return false;
        var owner = part.OwnerStructure;
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        return owner != null && owner.IsValid && owner.IsActive && !owner.IsDead && !owner.Dismantled
            && settlement != null && owner.Settlement == settlement && part.storageInteraction != null
            && part.repairContainer != null && part.storageInteraction.Container != null
            && part.storageInteraction.Container.Pointer == part.repairContainer.Pointer;
    }

    private static void SupplyPrefix(RepairPart __instance, out RepairScope __state)
    {
        __state = scope;
        scope = default;
        instance?.Hosted?.TryExecute(() =>
        {
            if (IsOwnedSupply(__instance)) scope = new(__instance, __instance.repairContainer.Pointer);
        });
    }

    private static Exception? SupplyFinalizer(Exception? __exception, RepairScope __state)
    {
        scope = __state;
        return __exception;
    }

    private static void FillRatioPostfix(ItemContainer __instance, bool __0, ref float __result)
    {
        if (!__0 || __instance is null || scope.Container != __instance.Pointer) return;
        var current = scope;
        scope = default;
        var qualify = false;
        instance?.Hosted?.TryExecute(() => qualify = IsOwnedSupply(current.Part));
        // Only the exact supply check sees the waiver. Native stage transitions consume
        // already deposited supplies and create the normal remaining repair-work budget.
        if (qualify) __result = 1f;
    }

    private static void RefreshPostfix(RepairPart __instance) => instance?.Hosted?.TryExecute(() =>
    {
        if (IsOwnedSupply(__instance)) __instance._RefreshPartsStatus();
    });

    public override void Tick()
    {
        if (!initialScanPending || GameObjectResolver.FindUnique<Settlement>() == null) return;
        initialScanPending = false;
        // Discover existing repairs once; native initialization/stage callbacks cover later repairs.
        foreach (var part in NativeObject.FindObjectsOfType<RepairPart>())
            if (IsOwnedSupply(part)) part._RefreshPartsStatus();
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        scope = default;
        initialScanPending = false;
        StatusReason = null;
        base.Disable();
    }
}
