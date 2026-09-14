# HutchASKA Stage 3: Items, Inventory, Crafting, and Building Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the WeMod-style item and construction feature set: infinite durability, no spoilage, no stack decrement on use, runtime item browser/give-item flow, free crafting, free building, and free repairs.

**Architecture:** Item mutation must go through ASKA's native inventory/item initialization paths whenever possible. Narrow Harmony patches may suppress known durability/expiration/resource-consumption operations, but broad inventory removal hooks are forbidden. Search/filter/presentation logic remains pure-core and testable; exact item creation, stack decrement, crafting consumption, and construction hooks are verified locally against the current ignored interop assemblies before coding.

**Tech Stack:** C# / .NET 6, BepInEx 6 IL2CPP, HarmonyX, Unity IMGUI, xUnit, current local `Assembly-CSharp.dll` and `SandSailorStudio.dll` interop references.

**Spec:** `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`

**Research:** `docs/research/2026-09-14-aska-interop-observations.md`

## Global Constraints

- Stages 1 and 2 must be complete first.
- Single-player guard applies to every mutating feature.
- Do not commit ASKA/BepInEx interop DLLs.
- Do not guess item creation or stack-decrement APIs; inspect local assemblies.
- Do not suppress all inventory removal globally to implement free crafting or Add Items On Use.
- Preserve native produced-item, building registration, repair completion, and save flows.
- If a safe narrow hook is not verified, mark only that feature incompatible and continue.

---

### Task 1: Implement infinite durability

**Files:**
- Create: `src/HutchASKA.Plugin/Items/InfiniteDurabilityFeature.cs`
- Create: `src/HutchASKA.Plugin/Items/Patches/ItemDurabilityPatch.cs`

**Interfaces:**
- Observed target: `SSSGame.ItemDurablilityProcess.Run(Item, ref float)`.

- [ ] **Step 1: Verify exact local signature**

Confirm spelling, namespace of `Item`, method static/instance status, return type, and meaning of the `ref float` parameter from the current local interop DLL.

Append the exact verified signature to the research note.

- [ ] **Step 2: Implement a narrow Harmony prefix/postfix**

The feature must prevent future durability loss only. Enabling it must not repair already damaged items. Disabling it must allow native durability loss immediately.

If the `ref float` represents delta/loss, prefer setting only that loss to zero rather than skipping unrelated processing.

- [ ] **Step 3: Local verification**

Use a partially damaged tool, record durability, perform enough actions to normally reduce it, disable the feature, and verify durability decreases again.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/Items docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add infinite item durability"
```

---

### Task 2: Implement no spoilage / infinite freshness

**Files:**
- Create: `src/HutchASKA.Plugin/Items/NoSpoilageFeature.cs`
- Create: `src/HutchASKA.Plugin/Items/Patches/ExpirationProcessPatch.cs`

**Interfaces:**
- Observed target: `SandSailorStudio.Inventory.ExpirationProcess.Run(Item, ref float)`.

- [ ] **Step 1: Verify exact signature and delta semantics locally**

Determine whether the `ref float` is elapsed time, freshness delta, or another processing value. Record findings.

- [ ] **Step 2: Implement narrow interception**

Prevent further freshness loss without arbitrarily setting every item to 100% freshness. Preserve current freshness while enabled.

- [ ] **Step 3: Verify with a perishable item**

Observe unchanged freshness across a period in which it normally declines, then disable and verify expiration resumes.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/Items docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add no-spoilage item control"
```

---

### Task 3: Discover and implement Add Items On Use

**Files:**
- Create: `src/HutchASKA.Plugin/Items/RetainItemsOnUseFeature.cs`
- Create: `src/HutchASKA.Plugin/Items/Patches/ItemConsumptionPatch.cs`
- Create: `docs/research/item-consumption-flow.md`

**Interfaces:**
- Behavior contract: using/consuming an item leaves the stack quantity unchanged.
- This feature does NOT duplicate arbitrary items and must not affect construction/crafting consumption unless the same action is explicitly a player-use/consume operation.

- [ ] **Step 1: Trace the current item-use flow locally**

Search the current interop assemblies for quantity/stack/remove/use/consume methods around `SandSailorStudio.Inventory.Item`, `ItemCollection`, and player interactions. Use local code search/reflection/decompiler tooling and document candidate call chains in `docs/research/item-consumption-flow.md`.

- [ ] **Step 2: Prove the narrowest interception point**

Choose a method that can distinguish player item use from crafting/building resource consumption. If no such safe point is found, stop this task with the feature marked `Incompatible`; do not patch a broad `RemoveItem` globally.

- [ ] **Step 3: Implement retention**

Preferred strategy order:

1. suppress only the decrement amount for a verified use/consume call;
2. preserve pre-use stack count and restore it after the native use action completes;
3. skip the decrement method only if the use side effects occur elsewhere and are proven to remain native.

- [ ] **Step 4: Verify across at least two item types**

Test one food/drink consumable and one other usable stack item if available. Confirm the use effect occurs but stack count remains unchanged. Confirm free crafting remains independent.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Items docs/research/item-consumption-flow.md
git commit -m "feat: retain item stacks on use"
```

---

### Task 4: Build the runtime item catalog and search model

**Files:**
- Create: `src/HutchASKA.Core/Items/ItemCatalogEntry.cs`
- Create: `src/HutchASKA.Core/Items/ItemCatalogSearch.cs`
- Create: `src/HutchASKA.Plugin/Items/AskaItemCatalog.cs`
- Test: `tests/HutchASKA.Core.Tests/Items/ItemCatalogSearchTests.cs`

**Interfaces:**
- `ItemCatalogEntry` exact shape:

```csharp
public sealed record ItemCatalogEntry(string Id, string DisplayName, string? InternalName);
```

- `ItemCatalogSearch.Filter(IReadOnlyList<ItemCatalogEntry> source, string? query)` returns case-insensitive matches by display/internal name and stable-orders results by display name then ID.

- [ ] **Step 1: Write search tests**

Cover null/empty query, case-insensitive search, internal-name match, and deterministic sort order.

- [ ] **Step 2: Implement pure search model**

Run:

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj --filter ItemCatalogSearchTests
```

Expected: PASS.

- [ ] **Step 3: Discover runtime item definitions locally**

Inspect current game registries/databases/manifests to find the authoritative item-definition collection. Do not create a hard-coded catalog copied from a website or another game build.

- [ ] **Step 4: Implement `AskaItemCatalog`**

Enumerate definitions into pure `ItemCatalogEntry` DTOs. Cache only definition metadata that is safe across a loaded session and invalidate on relevant game reload if needed.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Core/Items src/HutchASKA.Plugin/Items/AskaItemCatalog.cs tests/HutchASKA.Core.Tests/Items docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add runtime ASKA item catalog"
```

---

### Task 5: Implement safe Give Item / Give Stack

**Files:**
- Create: `src/HutchASKA.Plugin/Items/InventoryService.cs`
- Create: `src/HutchASKA.Plugin/Items/GiveItemFeature.cs`
- Create: `docs/research/item-creation-flow.md`

**Interfaces:**
- `InventoryService.TryGive(string itemId, int quantity, out string? error)`.
- Quantity must be clamped to a sane positive range and native stack-size behavior must be respected.

- [ ] **Step 1: Trace native item creation and insertion**

From the runtime definition found in Task 4, identify how ASKA itself creates an initialized `Item` and inserts it into the local player's inventory. Document factory/constructor/manifest APIs and full-inventory behavior.

- [ ] **Step 2: Reject malformed direct construction**

Do not use raw IL2CPP object allocation if it bypasses required definition, durability, freshness, metadata, or save initialization.

- [ ] **Step 3: Implement `TryGive`**

Use the native path. Return a human-readable error for unknown ID, invalid quantity, unresolved player inventory, or failed insertion.

- [ ] **Step 4: Verify save persistence**

Give a normal item, save/exit/reload, and confirm the item persists normally. If it does not, the creation path is not acceptable.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Items docs/research/item-creation-flow.md
git commit -m "feat: add native item give service"
```

---

### Task 6: Implement free crafting

**Files:**
- Create: `src/HutchASKA.Plugin/Crafting/FreeCraftingFeature.cs`
- Create: `src/HutchASKA.Plugin/Crafting/Patches/CraftingRequirementPatch.cs`
- Create: `src/HutchASKA.Plugin/Crafting/Patches/CraftingConsumptionPatch.cs`
- Create: `docs/research/crafting-flow.md`

**Interfaces:**
- Observed checks: `CraftInteraction.CheckOwnedRequirements()` and `CraftInteraction._CheckOwnedBlueprintManifest()`.
- Blueprint bypass remains a separate optional advanced toggle; do not bundle it silently into ordinary free crafting.

- [ ] **Step 1: Trace one complete normal crafting transaction**

Verify requirement check, resource consumption, produced-item creation, and completion call chain. Document exact local members.

- [ ] **Step 2: Patch material availability**

When free crafting is enabled, only the material-availability check should report success. Blueprint/unlock checks remain native unless the separate advanced feature is later enabled.

- [ ] **Step 3: Patch only crafting resource consumption**

Prevent ingredient loss specifically inside the verified crafting transaction. Do not disable shared inventory removal globally.

- [ ] **Step 4: Verify**

Craft an item with zero required materials and confirm native product creation. Disable the feature and confirm missing-material behavior returns.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Crafting docs/research/crafting-flow.md
git commit -m "feat: add free crafting"
```

---

### Task 7: Implement free building and free repairs

**Files:**
- Create: `src/HutchASKA.Plugin/Crafting/FreeBuildingFeature.cs`
- Create: `src/HutchASKA.Plugin/Crafting/FreeRepairsFeature.cs`
- Create: `src/HutchASKA.Plugin/Crafting/Patches/BuildSupplyPatch.cs`
- Create: `src/HutchASKA.Plugin/Crafting/Patches/RepairSupplyPatch.cs`
- Create: `docs/research/construction-flow.md`

**Interfaces:**
- Observed construction types: `BuildPart`, `BuildSite`, supply manifests, `IsSupplied`, `CheckSupplies`.

- [ ] **Step 1: Trace normal building supply flow**

Identify exact calls that decide whether a build part/site is supplied and where supplied resources are consumed. Separate building from repair paths if the game does.

- [ ] **Step 2: Implement Free Building**

Treat construction requirements as supplied while preserving placement, construction completion, entity registration, and save behavior.

- [ ] **Step 3: Implement Free Repairs separately**

Treat repair resources as available and non-consuming without making unrelated crafting/building calls free unless their own toggle is enabled.

- [ ] **Step 4: Verify persistence**

Build and repair under cheats, save/reload, and confirm ASKA recognizes resulting structures/items normally.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Crafting docs/research/construction-flow.md
git commit -m "feat: add free building and repairs"
```

---

### Task 8: Add Items and Crafting UI tabs

**Files:**
- Create: `src/HutchASKA.Plugin/UI/Tabs/ItemsTab.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/CraftingTab.cs`
- Modify: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`

- [ ] **Step 1: Items tab controls**

Required: Infinite Durability, No Spoilage, Add Items On Use, search field, item list, quantity input, Give Item button. Show give-item errors inline without throwing.

- [ ] **Step 2: Crafting & Building tab controls**

Required: Ignore Crafting Materials, Free Building, Free Repairs. Blueprint bypass appears only if a safe implementation exists and must be labeled Advanced/Experimental.

- [ ] **Step 3: Respect compatibility state**

Any locally unverified feature is rendered disabled with its reason; the rest remain usable.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/UI
git commit -m "feat: add item crafting and building UI"
```

---

### Task 9: Stage 3 verification and docs

**Files:**
- Create: `docs/testing/stage-3-smoke-test.md`
- Modify: `README.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Run core tests and local build**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj
.\scripts\Build-Local.ps1
.\scripts\Install-Local.ps1
```

- [ ] **Step 2: Smoke-test matrix**

Verify each feature independently and in common combinations:

1. durability frozen/resumes;
2. freshness frozen/resumes;
3. consuming an item preserves stack but still applies its native effect;
4. item catalog search works;
5. given item persists across save/reload;
6. free craft creates native product with zero mats;
7. normal crafting returns when disabled;
8. free build produces a normal persistent structure;
9. free repair works independently;
10. no repeating exceptions in BepInEx log.

- [ ] **Step 3: Repository hygiene check**

```bash
git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'
```

Expected: no proprietary/game binary tracked.

- [ ] **Step 4: Update README/CHANGELOG truthfully and commit**

```bash
git add README.md CHANGELOG.md docs/testing/stage-3-smoke-test.md
git commit -m "docs: verify item and crafting trainer stage"
```

## Stage 3 Exit Criteria

Stage 3 is complete when safe native item creation is proven, item/crafting/building features pass the documented local smoke tests or individually fail closed, all core tests pass, and the repository remains binary-clean.
