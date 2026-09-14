# Construction and repair inspection

Current build 25186770 Assembly-CSharp has `SSSGame.BuildPart.IsSupplied()` / `IsBuilt()` returning bool; `CheckSupplies()`, `StartSupply()`, `StartBuild()` and `FinishPart()` returning void. BuildSite exposes `_FillPartWithItems(BuildPart,List<ItemInfoQuantity>)`, `FinishBuild(int layerId)` and `_FinalizeBuild()`.

Repair is separate: `bool RepairInteraction.Use(IInteractionAgent)`, `float RepairPart.RepairStructure(float)`, `_RefreshRepairsStatus()`, `_ResetRepairsManifest()`, `_UpdateRepairsManifest()` and `ItemManifest _GenerateRepairsManifest(ItemManifest existingManifest)`. BuildPart.container and RepairPart.repairContainer are ItemManifestContainer instances with native SetManifest and SetDirtyManifeset (native spelling).

The generated wrappers do not reveal the native consumption/registration call chain. IsSupplied alone does not prove native completion can proceed without a real manifest. Directly invoking FinishPart or RepairStructure would bypass normal transaction behavior. Both controls are separately registered Incompatible; no global inventory-removal or forced-completion patches are installed. Build, independent repair, disable behavior and structure save/reload remain MANUAL VERIFICATION REQUIRED after isolated hooks are proven.
