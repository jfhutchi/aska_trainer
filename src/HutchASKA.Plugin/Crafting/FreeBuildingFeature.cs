extern alias UnityCore;
using HarmonyLib;
using HutchASKA.Core.Crafting;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;
using NativeObject = UnityCore::UnityEngine.Object;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeBuildingFeature(SinglePlayerGuard guard) : NativeFeature("building.free", "Free Building")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".building");
    private readonly Dictionary<(int Id, long Pointer, long Site, long Structure), BuildSupplyState> changed = new();
    private static FreeBuildingFeature? active;
    [ThreadStatic] private static SupplyReadScope? scope;
    private ItemManifest? emptyManifest;
    private bool scanPending;
    private long waived;
    private long reportedWaivers;

    public override CompatibilityResult ProbeCompatibility() =>
        new[] { "CheckSupplies", "IsSupplied", "IsBuilt" }.All(name =>
            AccessTools.DeclaredMethod(typeof(BuildPart), name, Type.EmptyTypes) is not null)
        && AccessTools.DeclaredMethod(typeof(ItemManifestContainer), "GetManifest", new[] { typeof(ManifestRequestType) })?.ReturnType == typeof(ItemManifest)
        && typeof(BuildSite).GetMethod("GetCurrentLayer", Type.EmptyTypes)?.ReturnType == typeof(BuildSite.BuildLayer)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native construction supply/work checks are unavailable.");

    public override bool TryEnable()
    {
        emptyManifest = new ItemManifest();
        active = this;
        foreach (var method in new[] { "CheckSupplies", "IsSupplied", "IsBuilt" })
            harmony.Patch(AccessTools.DeclaredMethod(typeof(BuildPart), method, Type.EmptyTypes),
                prefix: new HarmonyMethod(typeof(FreeBuildingFeature), nameof(BeginCheck)),
                finalizer: new HarmonyMethod(typeof(FreeBuildingFeature), nameof(EndCheck)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemManifestContainer), "GetManifest", new[] { typeof(ManifestRequestType) }),
            postfix: new HarmonyMethod(typeof(FreeBuildingFeature), nameof(ManifestPostfix)));
        scanPending = true;
        waived = 0;
        reportedWaivers = -1;
        StatusReason = "Waives missing construction materials; normal building work is still required.";
        return base.TryEnable();
    }

    public override void Tick()
    {
        if (scanPending)
        {
            if (!GameObjectResolver.FindUnique<Settlement>()) return;
            scanPending = false;
            // Existing sites are refreshed once; new sites already call CheckSupplies in StartSupply.
            foreach (var part in NativeObject.FindObjectsOfType<BuildPart>())
                if (IsCurrentPart(part)) part.CheckSupplies();
        }
        if (waived != reportedWaivers)
        {
            reportedWaivers = waived;
            StatusReason = $"Waives missing materials; normal building work required. Supply checks applied: {waived}.";
        }
    }

    private static bool IsCurrentPart(BuildPart part)
    {
        if (!part || !part.isActiveAndEnabled || part._forceBuilt || part.container is null
            || !part.buildInteraction || !part.supplyInteraction) return false;
        var site = part.buildSite;
        if (!site || !site!.session || !site.session.isMaster || site.GetCurrentLayer() != part._layer) return false;
        var structure = site.Structure;
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        return structure && structure.IsValid && !structure.IsDead && !structure.Dismantled
            && structure.Object.HasStateAuthority && settlement && structure.Settlement == settlement;
    }

    private static void BeginCheck(BuildPart __instance, out SupplyReadScope? __state)
    {
        __state = scope;
        scope = null;
        var feature = active;
        feature?.Hosted?.TryExecute(() =>
        {
            if (!IsCurrentPart(__instance)) return;
            var key = Identity(__instance);
            feature.changed.TryAdd(key, new(__instance.buildInteraction.enabled, __instance.supplyInteraction.enabled));
            scope = new(__instance.container.Pointer.ToInt64());
        });
    }

    private static Exception? EndCheck(Exception? __exception, SupplyReadScope? __state)
    {
        scope = __state;
        return __exception;
    }

    private static (int Id, long Pointer, long Site, long Structure) Identity(BuildPart part) =>
        (part.GetInstanceID(), part.Pointer.ToInt64(), part.buildSite.Pointer.ToInt64(), part.buildSite.Structure.Pointer.ToInt64());

    private static void ManifestPostfix(ItemManifestContainer __instance, ManifestRequestType __0, ref ItemManifest __result)
    {
        var feature = active;
        var check = scope;
        if (feature is null || check is null || __0 != ManifestRequestType.FULL) return;
        var replace = false;
        feature.Hosted?.TryExecute(() =>
        {
            replace = feature.emptyManifest is not null && check.TryConsume(__instance.Pointer.ToInt64());
            if (replace) feature.waived++;
        });
        // The inspected callers only read count. Original requirements and deposited items stay intact.
        if (replace) __result = feature.emptyManifest!;
    }

    public override void Disable()
    {
        active = null;
        scope = null;
        harmony.UnpatchSelf();
        scanPending = false;
        if (changed.Count > 0)
        {
            foreach (var part in NativeObject.FindObjectsOfType<BuildPart>(true))
            {
                if (!part || !part.buildSite || !part.buildSite.Structure) continue;
                var key = Identity(part);
                if (!changed.TryGetValue(key, out var original)) continue;
                if (!part._forceBuilt && part.buildInteraction && part.supplyInteraction)
                {
                    var restored = original.Restore(false, part.buildInteraction.enabled, part.supplyInteraction.enabled);
                    part.buildInteraction.enabled = restored.BuildEnabled;
                    part.supplyInteraction.enabled = restored.SupplyEnabled;
                    // Recompute actual supply progress and any legitimately completed material delivery.
                    if (guard.Refresh().Allowed && IsCurrentPart(part)) part.CheckSupplies();
                }
                changed.Remove(key);
            }
            changed.Clear();
        }
        emptyManifest = null;
        StatusReason = null;
        base.Disable();
    }
}
