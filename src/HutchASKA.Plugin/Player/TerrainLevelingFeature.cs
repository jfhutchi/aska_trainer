extern alias UnityCore;

using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using NativeObject = UnityCore::UnityEngine.Object;
using PreviewData = SSSGame.DynamicDimensionTemplate.DynamicDimensionStructurePreviewData;

namespace HutchASKA.Plugin.Player;

internal sealed class TerrainLevelingFeature(IPlayerContext players) : NativeFeature("player.terrain", "Leveling Area")
{
    private const string NormalName = "Item_Structures_TerrainLevelField";
    private const string LargeName = "Item_Structures_TerrainLevelFieldLarge";
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".terrain");
    private static TerrainLevelingFeature? instance;
    [ThreadStatic] private static RayLease? currentRay;
    private DynamicDimensionTemplate? sourceTemplate;
    private DynamicDimensionTemplate? privateTemplate;
    private PlayerBuilder? selectedBuilder;
    private readonly List<RayLease> borrowedPreviews = new();
    private int lastSize;
    public TerrainAreaSetting Size { get; } = new();

    private sealed record PointSnapshot(PointPlacement Point, Vector3 Position, Quaternion Rotation, bool Configured)
    {
        public void Restore()
        {
            Point.position = Position;
            Point.rotation = Rotation;
            Point._IsConfigured_k__BackingField = Configured;
        }
    }

    private sealed record RayLease(TerrainLevelingFeature Owner, DynamicDimensionsPlacementTool Tool,
        PreviewData Preview, DynamicDimensionsPlacement Placement, Vector3Int OriginalDimensions,
        PointSnapshot First, PointSnapshot Last, StructureTemplate Original,
        DynamicDimensionTemplate Borrowed, int Side, RayLease? Parent)
    {
        public bool Rejected { get; private set; }

        public void ReleaseTemplate()
        {
            try
            {
                if (Preview.structureTemplate == Borrowed) Preview.structureTemplate = Original;
                Owner.borrowedPreviews.Remove(this);
            }
            finally { currentRay = Parent; }
        }

        public bool RejectOversizedRectangle()
        {
            var current = Placement.gridSize;
            if (TerrainAreaSetting.Fits(current.x, current.z, Side)) return false;
            Rejected = true;
            // The native pivot uses the midpoint of the endpoints, so dimensions alone cannot
            // be shortened. Reject the whole cursor update, including its geometry and height.
            Placement.gridSize = OriginalDimensions;
            First.Restore();
            Last.Restore();
            return true;
        }
    }

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(DynamicDimensionsPlacementTool), "_OnBuildRayChanged", new[] { typeof(Ray) }) is not null
        && AccessTools.DeclaredMethod(typeof(DynamicDimensionsPlacementTool), "_OnSelect", Type.EmptyTypes) is not null
        && AccessTools.DeclaredMethod(typeof(DynamicDimensionTemplate), "UpdatePreview", new[] { typeof(StructurePreviewData), typeof(Placement) }) is not null
        && AccessTools.DeclaredMethod(typeof(PlayerBuilder), "set_StructureTemplate", new[] { typeof(StructureTemplate) }) is not null
        && typeof(DynamicDimensionTemplate).GetProperty(nameof(DynamicDimensionTemplate.maxNumberOfTiles))?.CanWrite == true
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Native leveling placement or grid template API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        lastSize = Size.Value;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PlayerBuilder), "set_StructureTemplate"),
            prefix: new HarmonyMethod(typeof(TerrainLevelingFeature), nameof(SelectPrefix)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(DynamicDimensionsPlacementTool), "_OnBuildRayChanged"),
            prefix: new HarmonyMethod(typeof(TerrainLevelingFeature), nameof(RayPrefix)),
            finalizer: new HarmonyMethod(typeof(TerrainLevelingFeature), nameof(RayFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(DynamicDimensionTemplate), "UpdatePreview"),
            prefix: new HarmonyMethod(typeof(TerrainLevelingFeature), nameof(PreviewPrefix)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(DynamicDimensionsPlacementTool), "_OnSelect"),
            prefix: new HarmonyMethod(typeof(TerrainLevelingFeature), nameof(ConfirmPrefix)));
        return base.TryEnable();
    }

    private bool IsLocal(PlayerBuilder? builder) => builder && builder!.gameObject.activeInHierarchy
        && builder.TryCast<PlayerBuilder_NewController>() is { } controller
        && controller._pInterAgent && players.TryGetLocalPlayer(out var player)
        && controller._pInterAgent.GetCharacter() == player;

    private static bool IsLeveling(DynamicDimensionTemplate? template) => template
        && template!.name is NormalName or LargeName && template.nodeStructure
        && template.nodeStructure.GetComponentInChildren<TerraformingGrid>(true);

    private static int NetworkElements(DynamicDimensionTemplate template)
    {
        if (template.nodeStructure.GetComponent<TerraformingGridState_512>()) return 6;
        return template.nodeStructure.GetComponent<TerraformingGridState_256>() ? 3 : 0;
    }

    private static DynamicDimensionTemplate? FindLarge(DynamicDimensionTemplate original)
    {
        DynamicDimensionTemplate? match = null;
        var originalGrid = original.nodeStructure.GetComponentInChildren<TerraformingGrid>(true);
        foreach (var candidate in UnityCore::UnityEngine.Resources.FindObjectsOfTypeAll<DynamicDimensionTemplate>())
        {
            if (!candidate || candidate.name != LargeName || !IsLeveling(candidate) || NetworkElements(candidate) != 6) continue;
            var grid = candidate.nodeStructure.GetComponentInChildren<TerraformingGrid>(true);
            if (candidate.placementTool != original.placementTool || grid.constants != originalGrid.constants
                || !grid.terraformingInteraction || !originalGrid.terraformingInteraction
                || grid.terraformingInteraction.moveset != originalGrid.terraformingInteraction.moveset) continue;
            if (match) return null;
            match = candidate;
        }
        return match;
    }

    private StructureTemplate? Select(PlayerBuilder builder, StructureTemplate? requested)
    {
        if (!IsLocal(builder)) return requested;
        selectedBuilder = builder;
        var template = requested?.TryCast<DynamicDimensionTemplate>();
        if (Size.Value <= 15 || !IsLeveling(template) || template!.name != NormalName) return requested;
        var large = FindLarge(template);
        if (!large)
        {
            StatusReason = "Large native grid unavailable; this leveling tool is limited to 15 x 15 tiles.";
            return requested;
        }
        StatusReason = "Uses the native large leveling grid. Each side is capped at the selected number of tiles.";
        return large;
    }

    private bool BeginRay(DynamicDimensionsPlacementTool tool, out RayLease? lease)
    {
        lease = null;
        if (Size.Value == 5 || !IsLocal(tool.Builder)) return true;
        // A nested ray or an outstanding failed restoration must not replace a borrowed clone.
        if (borrowedPreviews.Count != 0) return false;
        var preview = tool._currentPreviewData;
        var template = preview?.DynamicTemplate;
        if (preview is null || !IsLeveling(template) || tool._dynamicPlacement is null) return true;
        var placement = tool._dynamicPlacement;
        var points = placement.points;
        if (points is null || points.Length != 2 || points[0] is null || points[1] is null
            || points[0].anchor || points[1].anchor)
        {
            StatusReason = "Expanded area applies to standalone leveling; anchored placement keeps normal limits.";
            return true;
        }
        selectedBuilder = tool.Builder;
        if (Size.Value > 15 && template!.name == NormalName && FindLarge(template))
        {
            // The native setter destroys the old preview and creates the registered large prefab.
            // Stop this ray callback because its original preview has just been replaced.
            tool.Builder.StructureTemplate = template;
            return false;
        }
        var elements = NetworkElements(template!);
        if (elements == 0)
        {
            StatusReason = "The selected leveling grid has unknown storage capacity; normal limits remain active.";
            return true;
        }
        var side = TerrainAreaSetting.MaximumSide(Size.Value, elements);
        if (side < Size.Value)
            StatusReason = "Large native grid unavailable; this leveling tool is limited to 15 x 15 tiles.";
        if (!privateTemplate || sourceTemplate != template)
        {
            DestroyPrivateTemplate();
            sourceTemplate = template;
            privateTemplate = NativeObject.Instantiate(template!).Cast<DynamicDimensionTemplate>();
        }
        privateTemplate!.maxNumberOfTiles = side * side;
        lease = new(this, tool, preview, placement, placement.gridSize,
            new(points[0], points[0].position, points[0].rotation, points[0].IsConfigured),
            new(points[1], points[1].position, points[1].rotation, points[1].IsConfigured),
            preview.structureTemplate, privateTemplate, side, currentRay);
        borrowedPreviews.Add(lease);
        preview.structureTemplate = privateTemplate;
        currentRay = lease;
        return true;
    }

    public override void Tick()
    {
        if (lastSize == Size.Value) return;
        lastSize = Size.Value;
        CancelOwnedPreview();
        StatusReason = "Area changed. Reopen leveling to start a preview with the new size.";
    }

    private void CancelOwnedPreview()
    {
        if (IsLocal(selectedBuilder) && IsLeveling(selectedBuilder!.StructureTemplate?.TryCast<DynamicDimensionTemplate>()))
            selectedBuilder.StructureTemplate = null;
        selectedBuilder = null;
    }

    private void DestroyPrivateTemplate()
    {
        if (borrowedPreviews.Count != 0) return;
        if (privateTemplate) NativeObject.Destroy(privateTemplate);
        privateTemplate = null;
        sourceTemplate = null;
    }

    public override void Disable()
    {
        // Retry any reference restoration that failed in a finalizer. Keep its clone alive
        // if the retry also fails, rather than leaving a preview pointing to a destroyed asset.
        foreach (var preview in borrowedPreviews.ToArray()) preview.ReleaseTemplate();
        CancelOwnedPreview();
        DestroyPrivateTemplate();
        harmony.UnpatchSelf();
        instance = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Size.Reset(); }

    private static void SelectPrefix(PlayerBuilder __instance, ref StructureTemplate __0)
    {
        var feature = instance;
        if (feature is null) return;
        var selected = __0;
        if (feature.Hosted?.TryExecute(() => selected = feature.Select(__instance, selected)!) == true) __0 = selected;
    }

    private static bool RayPrefix(DynamicDimensionsPlacementTool __instance, out RayLease? __state)
    {
        __state = null;
        var feature = instance;
        if (feature is null) return true;
        if (feature.borrowedPreviews.Any(lease => lease.Tool.Pointer == __instance.Pointer)) return false;
        if (feature.Hosted?.State != FeatureState.Enabled) return true;
        RayLease? lease = null;
        var run = true;
        var succeeded = feature.Hosted.TryExecute(() => run = feature.BeginRay(__instance, out lease));
        __state = lease;
        return succeeded && run;
    }

    private static Exception? RayFinalizer(Exception? __exception, RayLease? __state)
    {
        if (__state is null) return __exception;
        try
        {
            __state.ReleaseTemplate();
            __state.RejectOversizedRectangle();
            if (__state.Rejected && __exception is null)
                __state.Tool._ValidatePlacement(false);
        }
        catch (Exception error)
        {
            __state.Owner.Hosted?.TryExecute(() => throw new InvalidOperationException("Leveling preview restoration failed.", error));
            return __exception ?? error;
        }
        return __exception;
    }

    private static void PreviewPrefix(StructurePreviewData __0, Placement __1)
    {
        var scope = currentRay;
        if (scope is null || __0.Pointer != scope.Preview.Pointer || __1.Pointer != scope.Placement.Pointer) return;
        scope.RejectOversizedRectangle();
    }

    private static bool ConfirmPrefix(DynamicDimensionsPlacementTool __instance)
    {
        var feature = instance;
        if (feature is null) return true;
        if (feature.borrowedPreviews.Any(lease => lease.Tool.Pointer == __instance.Pointer)) return false;
        if (feature.Hosted?.State != FeatureState.Enabled) return true;
        var allowed = true;
        var succeeded = feature.Hosted.TryExecute(() =>
        {
            if (feature.Size.Value == 5 || !feature.IsLocal(__instance.Builder)) return;
            var template = __instance._currentPreviewData?.DynamicTemplate;
            if (!IsLeveling(template) || __instance._dynamicPlacement is null) return;
            var points = __instance._dynamicPlacement.points;
            if (points is null || points.Length != 2 || points[0] is null || points[1] is null
                || points[0].anchor || points[1].anchor) return;
            var dimensions = __instance._dynamicPlacement.gridSize;
            allowed = TerrainAreaSetting.Fits(dimensions.x, dimensions.z,
                TerrainAreaSetting.MaximumSide(feature.Size.Value, NetworkElements(template!)));
            if (!allowed) feature.StatusReason = "Move the leveling cursor within the selected area before confirming.";
        });
        return succeeded && allowed;
    }
}
