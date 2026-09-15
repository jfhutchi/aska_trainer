# Local leveling area, Steam build 25186770

WITHDRAWN IN 0.1.5: the user crashed while extending the second side of a
20-tile preview. 10x10 was not attempted. Native storage capacity alone did not
establish preview safety. The expanded placement implementation and hooks have
been removed; the following records historical 0.1.4 research, not supported
runtime behavior. See [corrective evidence](../testing/0.1.5-corrective-retest.md).

## Crash cause correction, 2026-09-14 21:35 local

The 0.1.4 implementation checked the completed structure's grid capacity but missed a separate, smaller preview buffer. This invalidates the earlier claim that selecting the large structure made its preview safe for 20-by-20.

Both template assets 135783 and 135784 reference the same `nodePreview` asset 168190. That asset's GameObject 50108 is `PreviewGridLayout`, with `NetworkDynamicDimensionBuildingState_256` component 154813. Native `DynamicDimensionTemplate.CreatePreview` at RVA `0xB32D50` reads the template's **nodePreview** at field offset `0x170` and spawns it. The previously inspected `nodeStructure` field is at `0x168`. The large completed structure's 510-cell interaction map therefore does not expand the preview's 256-bit validity map.

The preview has a concrete native stack overwrite path. `NetworkDynamicDimensionBuildingState_256.UpdateGridValidityOnNetwork` at RVA `0x1057070` reserves `0x40` stack bytes after saving RDI, copies its 32-byte `BitSet256` to stack offset `0x20`, and loops over the full grid array length. For each valid cell it writes to `stack + 0x20 + (cellIndex / 64) * 8`, with **no 256-cell destination bound**. Cells 256-319 write over saved RDI; cells 320-383 write over the function's return address. Raising the placement cap to 400 exposed a native write past the preview buffer.

The matching local ASKA dump (21:35:15 local) records `C0000005`, a read at `0x4C000154`, and fault PC `GameAssembly.dll + 0x109FFF7`. That PC is in the middle of an instruction in unrelated code, consistent with a damaged return address. The active dimensions visible in the saved argument register's stack data are `(17, 4, 20)`: 340 horizontal cells, reaching the return-address overwrite range. Stack return candidates include `DynamicDimensionBuilding.SetDimensions + 0x54E`, `DynamicDimensionTemplate.UpdatePreview + 0x42C`, and `PlayerBuilder_NewController.Update + 0x1D8`. The dump remains outside the repository; no dump contents or personal filesystem paths are included in the project artifacts.

Confidence is high that the undersized shared preview validity buffer explains this crash. The unchecked write and the insufficient preview capacity are directly established by native code and shipped asset references; the crash's dimensions and corrupted instruction pointer agree with that mechanism. The sparse dump does not contain the relevant heap objects, so it does not permit reconstructing every individual validity bit that modified the return address. Template cloning is not needed to explain this failure, and this evidence does not establish cloning or smaller expanded areas as safe. All expanded terrain sizes remain withdrawn in 0.1.5; there is no request for another user reproduction.

## Historical implementation notes

This change expands the rectangular planned leveling field. It does not change the shovel's freehand brush or write terrain height data directly. The setting is a maximum side length in native grid tiles: normal 5, with 10, 15 and 20 supported. Each native tile spans 2 world coordinate units in this build; labels deliberately do not claim that the selected value is meters.

## Native and serialized evidence

Read-only LibCpp2IL metadata/address inspection and Capstone disassembly of the installed `GameAssembly.dll` established the following:

- `DynamicDimensionsPlacementTool._OnBuildRayChanged` at `0x180A5C3D0` resolves `_currentPreviewData.structureTemplate` at the start of the call. Cursor X/Z distances are divided by `gridConstants.tileSize` and rounded into signed grid dimensions. At `0x180A5C9F8`, the native code reads `DynamicDimensionTemplate.maxNumberOfTiles`; the following multiplication compares the absolute X/Z product to that limit. It declines an oversized cursor update. The limit is an area, so increasing it alone would allow a long 1-by-400 strip.
- `PlayerBuilder.set_StructureTemplate` at `0x180A73A20` destroys an old preview, stores the actual selected template, invokes that template's native `CreatePreview`, and changes its placement tool. **Corrected after the crash:** routing this setter selects the large completed structure, but both templates still spawn the same smaller preview described above.
- `PlayerBuilder.set_BuildPlacement` at `0x180A739C0` retains the native placement object. `DynamicDimensionTemplate.UpdatePreview` at `0x180B335F0` consumes its `gridSize` and uses the native dynamic building's `SetDimensions`. `PlacementGrid.SetDimensions` copies the width/depth directly, and `DynamicPlacementGrid` allocates width times depth cells. No extra border cells are required.
- The ordinary `TerraformingGridState_256.get__netInteractionMap` constructs a network array of **3** `BitSet256` elements. The large `_512` getter constructs **6**. Native `_OnInteractionMapChanged` at `0x180B218F0` multiplies that array length by **85** (`0x55`) for its actual capacity check, indexes elements by cell/85, and decodes three bits per cell. Actual capacities are therefore **255** and **510**, not a safe 400 cells in the ordinary variant.

Read-only UnityPy inspection of shipped `Aska_Data/sharedassets0.assets` found:

| Asset path ID | Asset | Relevant evidence |
| --- | --- | --- |
| 135783 | `Item_Structures_TerrainLevelField` | `DynamicDimensionTemplate`, serialized trailing cap 25, node structure 143489 |
| 135784 | `Item_Structures_TerrainLevelFieldLarge` | Same template type, trailing cap 25, node structure 143488 |
| 143489 / 12855 | ordinary structure/component and GameObject | `TerrainLevel_Field`, native `_256` grid state |
| 143488 / 12854 | large structure/component and GameObject | `TerrainLevel_FieldLarge`, native `_512` grid state |
| 137800 | `TerrainFieldPlacementTool` | Both templates point to this same native placement tool |
| 134804 | `CropGridConstants` | Both placement and terrain grids reference these constants; serialized tile size 2 |

Both terraforming interactions use the same `Moveset_TerraformLevel` asset (135075), with matching serialized configuration apart from references to their own prefab components. Their two `HeightmapTool` components (161495/161496) have byte-identical serialized configuration after the GameObject reference. The two terrain grids share constants and have matching work/geometry values. The feature selects the registered large prefab; it does not construct or resize a network behaviour or patch the packed network state.

## Scope and restoration

`TerrainLevelingFeature` matches only the two exact leveling template names and a real `TerraformingGrid`. A fresh local-player check requires the live active `PlayerBuilder_NewController` and its player interaction agent. Hosted feature execution applies the existing single-player and fault handling gates.

For sizes above 15, a local selection of the ordinary field is routed through the normal template setter to the native large field. Runtime discovery requires one exact large asset with the expected `_512` component, the same placement tool, the same grid constants and the same leveling moveset. An already open ordinary preview is replaced through that same native setter before processing another ray. If a matching large asset cannot be found, the ordinary grid is capped at 15 per side and the feature reports that limitation.

To lift the cursor cap, the feature clones the selected ScriptableObject template for its own use. Only the local preview's template reference is borrowed during one ray callback, with a Harmony finalizer restoring the exact original reference even on native exceptions. The shared template is never edited. The selected native template and its registered structure remain the source for construction and persistence. The clone is destroyed when replaced or the feature is disabled.

Reference restoration clears the temporary thread scope in a `finally` before rectangle cleanup. A failed restoration remains recorded for a lifecycle retry; its clone stays alive. While that lease is outstanding, the affected placement tool cannot process another ray or confirm. Disable retains these guards until reference cleanup succeeds. Cleanup errors are reported through the feature host, and a native exception already in flight is preserved. The final confirmation also checks both axes and the selected native grid's capacity.

Both signed X/Z dimensions are checked before any in-call preview update and again at ray exit, before later preview updates and confirmation. Oversized axes reject the entire cursor update: the previous grid dimensions and both endpoints' positions, rotations and configured state are restored. This matches the native area limit's refusal behavior. It is necessary because `SegmentPlacement.GetPivotPosition` at `0x180B3FB50` uses the endpoints' midpoint, while `GetHeightDifference` also uses the endpoints. Shortening only `gridSize` would leave a misplaced footprint or incorrect height validation. The native validation is refreshed after a rejected update.

The expansion applies to standalone leveling with two unanchored endpoints. Native anchored placement retains its normal limits; restoring anchor selection side effects is deliberately outside this feature. The native target height, validation, work interaction, materials and terrain application remain in the game path. The feature does not complete leveling work, multiply yields, alter world time, or mutate existing confirmed plots.

Changing the size or disabling the feature closes the local unconfirmed leveling preview. The user reopens leveling to start with the new limit; this prevents retaining a larger unconfirmed placement after lowering the setting. Existing confirmed plots continue their ordinary native work and persistence.

## Validation status

Pure tests cover the default/reset, input bounds, ordinary versus large packed capacity, and positive/negative axis rejection, including 1-by-400, reverse-direction selections and integer extremes. The parent task performs the integrated compilation and test run. Live game acceptance is still required: choose 10/15/20, draw a rectangle in both directions, verify a long thin oversized drag retains the last valid footprint and target, confirm 20-by-20 uses the large grid, perform ordinary leveling work at an edge and center, save/reload, and reduce the setting during an unconfirmed preview. Also verify cancellation, local player replacement, and the unchanged normal 5 setting. Static/native proof and automated tests do not establish that these live checks have passed.
