# Material-only free crafting: native evidence

Inspected local Steam build 25186770, Unity 6000.3.12f1, on 2026-09-14 using
LibCpp2IL and Capstone against GameAssembly.dll and its metadata. Raw native
artifacts remain outside this repository. The following are disassembly findings,
not gameplay acceptance.

## Native transaction

`CraftInteraction.CheckOwnedRequirements(Blueprint, IInteractionAgent)` at RVA
`0xB9D470` clears its static scratch `ItemManifest`, calls the supplied blueprint's
`FillPartsManifest`, then calls `_CheckOwnedBlueprintManifest` with that scratch
manifest and agent. A null blueprint returns false before reaching the helper.

`_CheckOwnedBlueprintManifest(ItemManifest, IInteractionAgent)` at RVA `0xB9DAE0`
only checks material quantities. It resolves the agent inventory, enumerates the
manifest, subtracts quantities in the agent's inventory, and then checks the
station inventory for any deficit. It returns false for an outstanding material
deficit and true for an empty/satisfied manifest. It has no blueprint-unlock,
station-skill, product creation, or completion logic.

`_OnCraftingSuccess(IInteractionAgent)` at RVA `0xB9DF30` constructs a fresh
`ItemManifest`, fills it from the selected blueprint, and calls that same helper.
If the helper returns true, it enumerates the SAME manifest for station/player
material removals. It then independently handles consumable blueprint-item
removal, native product insertion/world output, action result events, statistics,
deeds, and completion events. The temporary material manifest is not used again
after the material-removal loop. Product information and quantity come from the
native selected blueprint definition, independently of that manifest.

## Implemented hook

`FreeCraftingFeature` opens a thread-local scope only within those two inspected
caller methods and only for a positively identified local player interaction
agent whose native inventory matches the current local authoritative inventory.
The scope contains interaction/agent native identities, not retained objects.

The `_CheckOwnedBlueprintManifest` prefix consumes the scope once and clears the
temporary material manifest. The native helper still executes its ordinary
empty-manifest check. In the success transaction, the native removal loop then
has zero entries while product creation and all later steps proceed normally.
This needs no inventory-removal patch and does not synthesize completion.

Outside those two caller scopes the helper and manifests are unchanged. Each
caller has an exception-preserving finalizer restoring any outer scope. The
scope is consumed before returning to native material/product work, so later
callbacks cannot inherit permission to alter another manifest. Disabling removes
the feature's own patches and clears the scope. It does not change blueprint
assets, station inventory, recipe unlock flags, or saved cost settings.

The feature is limited to the normal local-player `CraftInteraction` path.
Villager crafting and different forging/cooking/processing implementations are
not automatically covered. Native consumable recipe/blueprint item costs remain
in effect; this toggle waives the recipe's material manifest only.

## Required acceptance

- Craft a normally unlocked recipe with zero materials; verify the native output
  quantity, initialized properties, normal animation/time and proficiency/events.
- Repeat with materials in player inventory and station storage; counts remain
  unchanged. Confirm any consumable blueprint item is still consumed normally.
- Confirm locked recipes and other station/agent eligibility remain restricted.
- Cancel during crafting, then disable and repeat: ordinary material requirements
  and consumption resume. Check multiple recipes and full-inventory output.
- Verify villager production and independent construction/repair remain native.
- Save/reload the output and verify stable native item persistence.

No gameplay test was performed by this worker. Root integration owns compilation
and combined tests after the parallel implementation work.

## Building and repairs remain separate

The native `BuildPart.IsSupplied` and `CheckSupplies` explicitly treat an empty
required manifest as supplied, but this is a persistent container, unlike the
crafting scratch manifest. `CheckSupplies` also switches supply/build interactions,
emits state events, and asks the build site to advance its layer. A blanket
manifest clear would discard/restyle already supplied state and requires a
validated disable/reload/partial-material strategy before being shipped.

`RepairPart._GenerateRepairsManifest` at RVA `0xB3DAC0` creates requirements from
the structure's dismantle manifest or a configured repair manifest. It accounts
for previously supplied materials, filtering and random selection, and may force
at least one repair material. Clearing this output alone does not establish the
repair container's completion, consumption, or restoration semantics. These
controls remain unavailable rather than bypassing native completion globally.
