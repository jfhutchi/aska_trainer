# HutchASKA Stage 4: Tribe, Final UI, Diagnostics, and Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete HutchASKA v1 with global tribe cheats, a safe individual villager editor, instant normal recruitment, full diagnostics/reset behavior, final README/license/release packaging, and a verified local release candidate.

**Architecture:** Tribe access is isolated behind an adapter because exact member names must be verified locally. Global tribe features iterate current registered villagers safely and never retain stale IL2CPP wrappers across unload/despawn. The individual editor stores a stable villager identity where available and resolves the live object on demand. Recruitment accelerates only ASKA's existing player-facing recruitment wait and never manually constructs villagers.

**Tech Stack:** C# / .NET 6, BepInEx 6 IL2CPP, HarmonyX, Unity IMGUI, xUnit, PowerShell packaging helpers, current local ASKA interop assemblies.

**Spec:** `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`

**Research:** `docs/research/2026-09-14-aska-interop-observations.md`

## Global Constraints

- Stages 1–3 must be complete first.
- Single-player guard applies to tribe mutation and recruitment acceleration.
- Do not create arbitrary villagers directly.
- Do not globally accelerate all tribe timers.
- Do not retain unsafe villager object references after unload/despawn.
- Global invincibility blocks damage rather than inflating health.
- Exact tribe members must be discovered from the local current interop assemblies; no guessed signatures.
- MIT `LICENSE`, root `README.md`, binary-clean public repository, and compatibility documentation are release requirements.

---

### Task 1: Map the current tribe/villager API locally

**Files:**
- Create: `docs/research/tribe-api-map.md`
- Create: `src/HutchASKA.Plugin/Tribe/ITribeContext.cs`
- Create: `src/HutchASKA.Plugin/Tribe/AskaTribeContext.cs`
- Create: `src/HutchASKA.Core/Tribe/VillagerSnapshot.cs`

**Interfaces:**
- `ITribeContext.GetCurrentVillagers()` returns live villager wrappers for immediate use only.
- `ITribeContext.TryResolveVillager(string stableId, out ...)` resolves an individual on demand.
- `VillagerSnapshot` contains only public-safe primitive/display data for UI.

- [ ] **Step 1: Inspect local interop assemblies**

Map exact current types/members for:

- registered tribe/population collection;
- villager identity/name;
- health/current/max health;
- food/hunger;
- water/thirst;
- warmth/temperature;
- energy;
- rest;
- happiness;
- age/lifetime;
- damage handling;
- normal recruitment/spawner timer/state.

Document exact namespaces, declaring types, property/field/method names, and any ambiguity in `docs/research/tribe-api-map.md`.

- [ ] **Step 2: Identify stable villager identity**

Prefer a game-provided GUID/entity ID/save ID. If none exists, document the best safe identity strategy and its limitations. Do not use a raw IL2CPP pointer as a persistent identity across unload/reload.

- [ ] **Step 3: Implement tribe context adapter**

The adapter must tolerate villagers appearing/disappearing and return an empty list rather than throwing during transitions.

- [ ] **Step 4: Local build and commit**

```powershell
.\scripts\Build-Local.ps1
```

```bash
git add docs/research/tribe-api-map.md src/HutchASKA.Plugin/Tribe src/HutchASKA.Core/Tribe
git commit -m "feat: map and adapt ASKA tribe runtime"
```

---

### Task 2: Add pure villager editor models and validation

**Files:**
- Create: `src/HutchASKA.Core/Tribe/VillagerEditRequest.cs`
- Create: `src/HutchASKA.Core/Tribe/VillagerValue.cs`
- Create: `src/HutchASKA.Core/Tribe/VillagerSearch.cs`
- Test: `tests/HutchASKA.Core.Tests/Tribe/VillagerEditTests.cs`
- Test: `tests/HutchASKA.Core.Tests/Tribe/VillagerSearchTests.cs`

**Interfaces:**
- `VillagerEditRequest` contains nullable requested changes: health fraction, food fraction, water fraction, warmth fraction, energy fraction, rest fraction, happiness fraction, and age when supported.
- Fractions are clamped to `[0,1]`; age validation uses native min/max discovered in Task 1 rather than an invented human-age bound.

- [ ] **Step 1: Write validation tests**

Test null/no-op request, fraction clamping, and deterministic villager search by name/ID.

- [ ] **Step 2: Implement models**

Keep these classes independent from ASKA/Unity references.

- [ ] **Step 3: Run tests**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj --filter Tribe
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Core/Tribe tests/HutchASKA.Core.Tests/Tribe
git commit -m "feat: add villager editor models"
```

---

### Task 3: Implement global villager invincibility

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/VillagerGodModeFeature.cs`
- Create: `src/HutchASKA.Plugin/Tribe/Patches/VillagerDamagePatch.cs`

- [ ] **Step 1: Verify damage target locally**

Determine whether villagers use `SSSGame.Character.TakeDamage`, a subclass override, or another damage pipeline. The patch must distinguish tribe villagers from player/enemies/animals.

- [ ] **Step 2: Implement narrow damage block**

Return/suppress native damage only for currently registered tribe villagers when the toggle is enabled and single-player guard is allowed.

Do not set villager health to huge values.

- [ ] **Step 3: Verify**

Damage a villager under normal conditions if safely reproducible, enable invincibility and verify no health loss, disable and verify native behavior resumes.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/Tribe
git commit -m "feat: add villager invincibility"
```

---

### Task 4: Implement global tribe needs controls

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/TribeNeedsFeatureSet.cs`
- Create: `src/HutchASKA.Plugin/Tribe/VillagerAttributeAdapter.cs`

**Required toggles:**
- No Hunger
- No Thirst
- Temperature Immunity
- Infinite Energy
- Full Rest
- Max Happiness
- Freeze Aging

**Required one-shot actions:**
- Heal Entire Tribe
- Restore All Needs

- [ ] **Step 1: Bind each need to its verified native field/property**

Use native min/max/current semantics. For hunger/water/rest/energy/happiness, maintain the native desirable endpoint, not an arbitrary constant.

For warmth/temperature, determine the safe/neutral range from the actual game semantics before assigning.

- [ ] **Step 2: Implement global iteration safely**

Each tick/update must obtain the current villager list from `ITribeContext`; do not retain the list or wrappers beyond the current operation.

- [ ] **Step 3: Handle new villagers automatically**

A newly registered villager must receive active global need protections on the next update without requiring the user to toggle the feature off/on.

- [ ] **Step 4: Implement one-shot actions**

`Heal Entire Tribe` sets health through native setters/fields to native max. `Restore All Needs` performs one immediate restoration without enabling persistent toggles.

- [ ] **Step 5: Verify and commit**

```powershell
.\scripts\Build-Local.ps1
```

```bash
git add src/HutchASKA.Plugin/Tribe docs/research/tribe-api-map.md
git commit -m "feat: add global tribe needs controls"
```

---

### Task 5: Implement individual villager editor

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/VillagerEditorService.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/TribeTab.cs`

**Interfaces:**
- `VillagerEditorService.TrySnapshot(string stableId, out VillagerSnapshot snapshot, out string? error)`.
- `VillagerEditorService.TryApply(string stableId, VillagerEditRequest request, out string? error)`.

- [ ] **Step 1: Implement on-demand resolve**

Every snapshot/apply operation resolves the current live villager from stable identity. A stale/unavailable villager returns a human-readable error and refreshes the list.

- [ ] **Step 2: Implement editor actions**

Required buttons: `Heal`, `Max Needs`, `Apply Changes`.

Fields: Health, Food, Water, Warmth, Energy, Rest, Happiness, Age only when verified safe.

If age mutation cannot be proven safe for save/AI behavior, render Age read-only and state why in diagnostics rather than forcing it.

- [ ] **Step 3: Build Tribe tab global + individual sections**

The tab must contain global toggles/actions above the individual selector/editor. Search/filter is optional if the list is short but should use `VillagerSearch` if present.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/Tribe src/HutchASKA.Plugin/UI/Tabs/TribeTab.cs
git commit -m "feat: add individual villager editor"
```

---

### Task 6: Implement instant normal villager recruitment

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/InstantRecruitmentFeature.cs`
- Create: `src/HutchASKA.Plugin/Tribe/Patches/RecruitmentTimerPatch.cs`
- Modify: `docs/research/tribe-api-map.md`

**Behavior contract:**
- No `Spawn Villager` button.
- Use ASKA's normal player-facing recruitment/spawner workflow.
- Reduce only the verified wait/cooldown to immediate or the smallest safe native duration.
- Preserve name generation, traits, AI init, population registration, bookkeeping, and save persistence.

- [ ] **Step 1: Trace one full normal recruitment lifecycle**

Record start trigger, timer/cooldown state, completion method/event, villager creation, registration, and reset/rearm behavior.

- [ ] **Step 2: Identify the narrow timer value/check**

Prefer changing the remaining wait/completion condition rather than directly invoking the villager constructor/spawn method.

- [ ] **Step 3: Implement the feature**

When enabled, only the normal recruitment wait is accelerated. Disabling must restore the current/native delay for subsequent recruitment cycles without corrupting an already completed cycle.

- [ ] **Step 4: Verify lifecycle and persistence**

Recruit at least two villagers using the normal UI/mechanism with the feature enabled. Verify distinct normal names/traits (if applicable), correct population count, normal AI, save/reload persistence, and no repeated runaway spawning.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Tribe docs/research/tribe-api-map.md
git commit -m "feat: add instant normal villager recruitment"
```

---

### Task 7: Complete Advanced and Diagnostics tabs

**Files:**
- Create: `src/HutchASKA.Plugin/UI/Tabs/AdvancedTab.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/DiagnosticsTab.cs`
- Modify: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`
- Modify: `src/HutchASKA.Plugin/Infrastructure/DiagnosticsService.cs`

**Required Advanced controls:**
- restore enabled states on launch setting;
- reload configuration;
- re-scan runtime compatibility;
- Reset All;
- logging verbosity.

**Required Diagnostics fields:**
- HutchASKA version;
- ASKA build/version when detectable;
- Unity version;
- BepInEx version;
- current session-mode decision;
- feature-by-feature state (`Enabled`, `Disabled`, `Blocked`, `Incompatible`, `Faulted`);
- concise last-error/reason.

- [ ] **Step 1: Implement Reset All semantics**

Reset All disables every active feature, restores captured native movement/time/game-speed values, clears transient editor selection/errors, and leaves the UI available. It must not require a game restart where reversible runtime behavior exists.

- [ ] **Step 2: Implement re-scan compatibility**

Re-scan is allowed only for features designed to safely probe again. It must not stack duplicate Harmony patches.

- [ ] **Step 3: Implement diagnostics rendering**

No full stack traces in the UI. Full exception details remain in the BepInEx log.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/UI src/HutchASKA.Plugin/Infrastructure
git commit -m "feat: complete advanced and diagnostics UI"
```

---

### Task 8: Add packaging and local release script

**Files:**
- Create: `scripts/Package-Release.ps1`
- Modify: `.gitignore`
- Modify: `README.md`

**Interfaces:**
- Output: `artifacts/HutchASKA-v<version>.zip` containing only redistributable HutchASKA files.

- [ ] **Step 1: Implement packaging**

Package layout:

```text
HutchASKA/
├── HutchASKA.Plugin.dll
├── README.txt
└── LICENSE
```

The ZIP must NOT contain ASKA DLLs, generated interop DLLs, BepInEx core binaries, PDBs unless explicitly intended, or local paths/config secrets.

- [ ] **Step 2: Add package validation**

Before creating the ZIP, fail if the staging directory contains forbidden names/patterns including `Assembly-CSharp.dll`, `SandSailorStudio.dll`, `BepInEx.Core.dll`, `0Harmony.dll`, or `Il2CppInterop.*.dll`.

- [ ] **Step 3: Document release install**

README must tell users to install BepInEx separately and copy `HutchASKA.Plugin.dll` into `ASKA\BepInEx\plugins\HutchASKA\`.

- [ ] **Step 4: Commit**

```bash
git add scripts/Package-Release.ps1 .gitignore README.md
git commit -m "build: add clean local release packaging"
```

---

### Task 9: Final README, license, changelog, and attribution audit

**Files:**
- Modify: `README.md`
- Verify: `LICENSE`
- Modify: `CHANGELOG.md`
- Create: `THIRD_PARTY_NOTICES.md`

- [ ] **Step 1: Verify MIT license exists and is standard**

Required copyright line:

```text
Copyright (c) 2026 jfhutchi
```

Do not replace it with an invented legal name.

- [ ] **Step 2: Complete README sections**

README must accurately include: status, exact tested compatibility, complete feature matrix, requirements, BepInEx setup link/instructions, release install, source build, local ASKA reference setup, controls/hotkeys, single-player guard, achievements statement, diagnostics/log path, ASKA update/rebuild procedure, known limitations, contribution guidance, license, and third-party/game-assets disclaimer.

- [ ] **Step 3: Add `THIRD_PARTY_NOTICES.md`**

State that HutchASKA does not redistribute ASKA binaries/assets and that ASKA/game names/trademarks remain property of their respective owners. List any NuGet/source dependencies actually added by implementation and their licenses based on package metadata; do not invent dependencies that are not used.

- [ ] **Step 4: Update changelog**

Move verified v1 features from `[Unreleased]` into a release section only after final testing. Features that remain incompatible on the target build must be called out honestly.

- [ ] **Step 5: Commit**

```bash
git add README.md LICENSE CHANGELOG.md THIRD_PARTY_NOTICES.md
git commit -m "docs: prepare HutchASKA v1 public documentation"
```

---

### Task 10: Full v1 verification and release candidate

**Files:**
- Create: `docs/testing/v1-release-checklist.md`
- Modify: `CHANGELOG.md` if all acceptance criteria pass.

- [ ] **Step 1: Back up a single-player save**

Use a disposable or backed-up save for mutation testing.

- [ ] **Step 2: Run automated verification**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj
.\scripts\Build-Local.ps1
.\scripts\Package-Release.ps1
```

Expected: tests PASS, plugin builds, clean package produced.

- [ ] **Step 3: Run complete in-game matrix**

Verify at minimum:

- F8 UI and F1/F2/F5 hotkeys;
- player God Mode;
- stamina;
- hunger/thirst;
- temperature if compatible;
- movement multiplier/reset;
- durability;
- spoilage;
- retain items on use;
- item browser + give + save persistence;
- free crafting;
- free building;
- free repairs;
- freeze/+1/-1 time;
- game speed/reset;
- global villager invincibility;
- each global villager need toggle;
- Heal Entire Tribe / Restore All Needs;
- per-villager read/edit/apply;
- instant normal recruitment with no runaway repeats;
- Reset All;
- session guard blocks gameplay cheats when single-player cannot be confirmed;
- feature failures remain isolated;
- Steam achievement code paths are untouched by HutchASKA.

- [ ] **Step 4: Check logs after an extended session**

Ensure there is no high-frequency exception spam, stale-object error loop, or repeated patch application.

- [ ] **Step 5: Repository/package hygiene**

```bash
git status --short
git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'
git grep -n -E 'Assembly-CSharp\.dll|SandSailorStudio\.dll' -- ':!README.md' ':!libs/README.md' ':!docs/**'
```

Tracked binary command must show no proprietary/game binaries. Documentation references are allowed.

Inspect release ZIP contents manually or with PowerShell and verify no forbidden dependency DLLs are present.

- [ ] **Step 6: Record tested versions**

Write the exact ASKA build observed at final test time, Unity version, BepInEx version, and HutchASKA version in `docs/testing/v1-release-checklist.md`. If ASKA updated since the initial `25186770` target, do not silently claim the old build; record both the originally targeted and actually tested build.

- [ ] **Step 7: Final commit**

```bash
git add docs/testing/v1-release-checklist.md CHANGELOG.md
git commit -m "test: verify HutchASKA v1 release candidate"
```

## Stage 4 Exit Criteria

HutchASKA v1 is release-ready only when the README and MIT license are present, the package contains only redistributable HutchASKA files, the current local ASKA build is recorded, all supported features pass the release matrix, unsupported features visibly fail closed, no multiplayer/co-op mutation is enabled, and no proprietary game binary is tracked or shipped.
