# HutchASKA Stage 4: Tribe, Final UI, Diagnostics, and Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete HutchASKA v1 with global tribe controls, a per-villager editor, instant normal recruitment, final diagnostics/reset behavior, release packaging, README/license audit, and a verified local release candidate.

**Architecture:** Raw ASKA villager objects never cross the `AskaTribeContext` boundary. The rest of the plugin works with stable villager IDs, primitive snapshots, and edit requests. This avoids stale IL2CPP references and allows exact game-specific types/members to be discovered locally without contaminating core/UI contracts. Recruitment changes only the verified normal recruitment wait/cooldown and never manually constructs villagers.

**Tech Stack:** C# / .NET 6, BepInEx 6 IL2CPP, HarmonyX, Unity IMGUI, xUnit, PowerShell, current local ASKA interop assemblies.

**Spec:** `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`

**Research:** `docs/research/2026-09-14-aska-interop-observations.md`

## Global Constraints

- Stages 1–3 must be complete first.
- All tribe mutation/recruitment features require a confirmed local single-player session.
- Do not manually construct/spawn villagers.
- Do not accelerate unrelated tribe timers.
- Do not retain raw villager wrappers/pointers across frames, unloads, despawns, or save reloads.
- Villager invincibility blocks damage rather than inflating health.
- Exact game members are discovered from local current interop assemblies; do not guess signatures.
- The public repository and release archive must not contain ASKA or generated interop binaries.

---

### Task 1: Define pure tribe contracts and map the current ASKA tribe API

**Files:**
- Create: `src/HutchASKA.Core/Tribe/VillagerSnapshot.cs`
- Create: `src/HutchASKA.Core/Tribe/VillagerEditRequest.cs`
- Create: `src/HutchASKA.Core/Tribe/VillagerSearch.cs`
- Create: `src/HutchASKA.Plugin/Tribe/ITribeContext.cs`
- Create: `src/HutchASKA.Plugin/Tribe/AskaTribeContext.cs`
- Create: `docs/research/tribe-api-map.md`
- Test: `tests/HutchASKA.Core.Tests/Tribe/VillagerSearchTests.cs`
- Test: `tests/HutchASKA.Core.Tests/Tribe/VillagerEditRequestTests.cs`

**Interfaces:**

Use these exact public/core shapes:

```csharp
namespace HutchASKA.Core.Tribe;

public sealed record VillagerSnapshot(
    string StableId,
    string DisplayName,
    float HealthFraction,
    float FoodFraction,
    float WaterFraction,
    float WarmthFraction,
    float EnergyFraction,
    float RestFraction,
    float HappinessFraction,
    float? Age);

public sealed record VillagerEditRequest(
    float? HealthFraction = null,
    float? FoodFraction = null,
    float? WaterFraction = null,
    float? WarmthFraction = null,
    float? EnergyFraction = null,
    float? RestFraction = null,
    float? HappinessFraction = null,
    float? Age = null);
```

Use this exact plugin boundary:

```csharp
internal interface ITribeContext
{
    IReadOnlyList<string> GetCurrentVillagerIds();
    bool TrySnapshot(string stableId, out VillagerSnapshot? snapshot, out string? error);
    bool TryApply(string stableId, VillagerEditRequest request, out string? error);
    bool TryHeal(string stableId, out string? error);
    bool IsCurrentVillager(object candidate);
}
```

`AskaTribeContext` owns all casts/access to discovered raw ASKA types.

- [ ] **Step 1: Write pure validation/search tests first**

Test case-insensitive name/ID search, deterministic sorting, fraction clamping to `[0,1]`, and a no-op request where all properties are null.

- [ ] **Step 2: Inspect local current interop assemblies**

Map exact current members for population/registered villagers, stable identity/name, health/max health, food, water, warmth, energy, rest, happiness, age/lifetime, damage, and recruitment timer/lifecycle. Record namespaces/signatures in `docs/research/tribe-api-map.md`.

- [ ] **Step 3: Choose stable identity**

Prefer a game-provided GUID/entity/save ID. If none exists, document the chosen compound identity and its limitations. Never persist an IL2CPP pointer as identity.

- [ ] **Step 4: Implement `AskaTribeContext`**

Every operation resolves the current live game object inside the method, performs the read/write, and discards the raw wrapper. During transitions, return an empty ID list or a clear error rather than throwing.

- [ ] **Step 5: Run tests/build and commit**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj --filter Tribe
.\scripts\Build-Local.ps1
```

```bash
git add src/HutchASKA.Core/Tribe src/HutchASKA.Plugin/Tribe tests/HutchASKA.Core.Tests/Tribe docs/research/tribe-api-map.md
git commit -m "feat: add safe tribe adapter and editor contracts"
```

---

### Task 2: Implement global villager invincibility

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/VillagerGodModeFeature.cs`
- Create: `src/HutchASKA.Plugin/Tribe/Patches/VillagerDamagePatch.cs`

- [ ] **Step 1: Verify the exact villager damage call locally**

Determine whether villagers use `SSSGame.Character.TakeDamage`, a subclass override, or another path. Record the target in the tribe API map.

- [ ] **Step 2: Implement narrow damage interception**

Suppress native damage only when the feature is enabled, single-player is allowed, and `ITribeContext.IsCurrentVillager(__instance)` is true. Player/enemy/animal damage must remain untouched.

- [ ] **Step 3: Verify disable restores native damage and commit**

```bash
git add src/HutchASKA.Plugin/Tribe docs/research/tribe-api-map.md
git commit -m "feat: add villager invincibility"
```

---

### Task 3: Implement global tribe needs and one-shot restoration

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/TribeNeedsFeatureSet.cs`

**Required persistent toggles:** Invincible Villagers (Task 2), No Hunger, No Thirst, Temperature Immunity, Infinite Energy, Full Rest, Max Happiness, Freeze Aging.

**Required one-shot actions:** Heal Entire Tribe, Restore All Needs.

- [ ] **Step 1: Map desired values through native ranges**

For health/food/water/energy/rest/happiness use the game-defined desirable endpoint. Determine a safe neutral warmth value/range from native behavior; do not assume max warmth is neutral.

- [ ] **Step 2: Implement current-ID iteration**

Each maintenance pass calls `GetCurrentVillagerIds()` and applies an edit request by ID. Never cache raw villagers.

- [ ] **Step 3: Implement Freeze Aging**

Patch/neutralize only the verified age/lifetime progression path. If no narrow reversible hook is verified, mark only Freeze Aging incompatible.

- [ ] **Step 4: Implement one-shot actions**

`Heal Entire Tribe` calls `TryHeal` for each current ID. `Restore All Needs` sends one edit request restoring verified need values without enabling persistent toggles.

- [ ] **Step 5: Verify new recruits inherit active global toggles automatically and commit**

```bash
git add src/HutchASKA.Plugin/Tribe
git commit -m "feat: add global tribe needs controls"
```

---

### Task 4: Implement the individual villager editor and Tribe tab

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/VillagerEditorService.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/TribeTab.cs`
- Modify: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`

**Interfaces:**

```csharp
internal sealed class VillagerEditorService
{
    public bool TryGet(string stableId, out VillagerSnapshot? snapshot, out string? error);
    public bool TryApply(string stableId, VillagerEditRequest request, out string? error);
    public bool TryHeal(string stableId, out string? error);
}
```

- [ ] **Step 1: Build global section**

Render all global tribe toggles plus Heal Entire Tribe and Restore All Needs. Disabled/incompatible controls show their diagnostic reason.

- [ ] **Step 2: Build individual selector/editor**

Select by stable ID/display name, resolve a fresh snapshot when selected/refreshed, and expose Health, Food, Water, Warmth, Energy, Rest, Happiness, and Age only if safe.

Required buttons: Heal, Max Needs, Apply Changes.

- [ ] **Step 3: Handle disappearing villagers**

If `TryGet` fails because the villager left/unloaded/died, clear selection, refresh IDs, and show a concise message. Do not retain the old object.

- [ ] **Step 4: Age safety**

If age mutation is not proven safe for AI/save behavior, render age read-only and state the reason; do not force support.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Tribe src/HutchASKA.Plugin/UI
git commit -m "feat: add global and per-villager tribe UI"
```

---

### Task 5: Implement instant normal recruitment

**Files:**
- Create: `src/HutchASKA.Plugin/Tribe/InstantRecruitmentFeature.cs`
- Create: `src/HutchASKA.Plugin/Tribe/Patches/RecruitmentTimerPatch.cs`
- Modify: `docs/research/tribe-api-map.md`

- [ ] **Step 1: Trace one full normal recruitment lifecycle**

Document start trigger, wait/cooldown value/check, completion path, name/trait generation, AI initialization, population registration, save state, and timer rearm.

- [ ] **Step 2: Identify only the wait/cooldown hook**

Prefer setting remaining wait to zero/minimum or making the verified timer-complete check succeed. Do not invoke villager constructors or internal spawn routines directly.

- [ ] **Step 3: Implement feature**

While enabled, normal player-facing recruitment completes immediately or at the smallest proven safe delay. Unrelated settlement timers remain unchanged.

- [ ] **Step 4: Verify no runaway loop**

Recruit at least two villagers through ASKA's normal mechanism. Verify names/traits, correct population, normal AI, save/reload persistence, and that recruitment does not continuously repeat without the normal trigger.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Tribe docs/research/tribe-api-map.md
git commit -m "feat: add instant normal villager recruitment"
```

---

### Task 6: Complete Advanced/Diagnostics and Reset All

**Files:**
- Create: `src/HutchASKA.Plugin/UI/Tabs/AdvancedTab.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/DiagnosticsTab.cs`
- Modify: `src/HutchASKA.Plugin/Infrastructure/DiagnosticsService.cs`
- Modify: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`

- [ ] **Step 1: Add Advanced controls**

Required: Restore Enabled States on Launch setting, Reload Configuration, Re-scan Compatibility, Reset All, logging verbosity.

- [ ] **Step 2: Implement Reset All**

Disable every feature, restore captured native movement/time/game-speed values, stop persistent tribe maintenance, clear editor selection/errors, and leave F8/diagnostics available.

- [ ] **Step 3: Complete Diagnostics**

Show HutchASKA version, detected ASKA build/version, Unity version, BepInEx version, session decision, and every feature state/reason. Stack traces remain in `BepInEx\LogOutput.txt`, not the UI.

- [ ] **Step 4: Ensure re-scan cannot duplicate Harmony patches and commit**

```bash
git add src/HutchASKA.Plugin/UI src/HutchASKA.Plugin/Infrastructure
git commit -m "feat: complete trainer diagnostics and reset controls"
```

---

### Task 7: Add clean release packaging

**Files:**
- Create: `scripts/Package-Release.ps1`
- Modify: `.gitignore`
- Modify: `README.md`

- [ ] **Step 1: Create release ZIP**

Output: `artifacts/HutchASKA-v<version>.zip` with:

```text
HutchASKA/
├── HutchASKA.Plugin.dll
├── README.txt
└── LICENSE
```

- [ ] **Step 2: Reject forbidden dependencies from package**

Fail packaging if staging contains `Assembly-CSharp.dll`, `SandSailorStudio.dll`, `BepInEx.Core.dll`, `0Harmony.dll`, any `Il2CppInterop*.dll`, ASKA assets/bundles, or user-specific config/secrets.

- [ ] **Step 3: Document release installation**

README instructs users to install compatible BepInEx separately and copy only `HutchASKA.Plugin.dll` to `ASKA\BepInEx\plugins\HutchASKA\`.

- [ ] **Step 4: Commit**

```bash
git add scripts/Package-Release.ps1 .gitignore README.md
git commit -m "build: add clean HutchASKA release packaging"
```

---

### Task 8: Final README, MIT license, notices, and release verification

**Files:**
- Modify: `README.md`
- Verify: `LICENSE`
- Modify: `CHANGELOG.md`
- Create: `THIRD_PARTY_NOTICES.md`
- Create: `docs/testing/v1-release-checklist.md`

- [ ] **Step 1: Verify license**

`LICENSE` is the standard MIT License for HutchASKA-authored source with:

```text
Copyright (c) 2026 jfhutchi
```

- [ ] **Step 2: Audit README completeness**

README must contain status, tested compatibility, feature matrix, requirements, BepInEx setup, release install, source build, local ASKA references, hotkeys/controls, single-player safety, achievements statement, update/rebuild procedure, diagnostics/logs, known limitations, contributing, license, and third-party/game-assets disclaimer.

- [ ] **Step 3: Create third-party notices from actual dependencies**

List only dependencies actually used and their verified licenses. State that ASKA binaries/assets are not redistributed and game/trademark rights belong to their respective owners.

- [ ] **Step 4: Run automated verification**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj
.\scripts\Build-Local.ps1
.\scripts\Package-Release.ps1
```

- [ ] **Step 5: Run full backed-up single-player smoke matrix**

Verify all supported player, world, item, crafting/building, tribe, editor, recruitment, hotkey, Reset All, and single-player-guard behaviors. Do not mark a feature supported solely because it compiles.

- [ ] **Step 6: Record actual final compatibility**

In `docs/testing/v1-release-checklist.md` record HutchASKA version, actual ASKA Steam build observed at test time, Unity version, BepInEx version, each feature's result, and known limitations. If ASKA has updated beyond build `25186770`, record both the original target and the actually tested build.

- [ ] **Step 7: Audit repository/package hygiene**

```bash
git status --short
git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'
```

Expected: no proprietary/game binary tracked. Inspect the release ZIP and confirm it contains only HutchASKA redistributable files.

- [ ] **Step 8: Final documentation commit**

```bash
git add README.md LICENSE CHANGELOG.md THIRD_PARTY_NOTICES.md docs/testing/v1-release-checklist.md
git commit -m "test: verify HutchASKA v1 release candidate"
```

## Stage 4 Exit Criteria

HutchASKA v1 is release-ready only when README and MIT license are present, supported features pass the local release matrix, unsupported features visibly fail closed, instant recruitment uses only the normal ASKA lifecycle, no multiplayer/co-op mutation is enabled, actual tested versions are documented, and neither Git nor the release ZIP contains proprietary ASKA/generated interop binaries.
