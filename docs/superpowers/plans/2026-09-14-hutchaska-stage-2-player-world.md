# HutchASKA Stage 2: Player and World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the first genuinely useful local trainer build: player God Mode, infinite stamina, hunger/thirst controls, temperature protection, movement multiplier, world-time freeze/adjustment, game-speed controls, hotkeys, and a working F8 Player/World UI.

**Architecture:** Keep all game-specific access behind narrow plugin adapters that call the current local ASKA IL2CPP interop assemblies. Pure decision/state logic remains in `HutchASKA.Core` and is unit-tested without ASKA. Runtime feature classes register through the Stage 1 feature registry and must fail closed when the session is not confirmed single-player or when a target member cannot be validated.

**Tech Stack:** C# / .NET 6, BepInEx 6 IL2CPP, HarmonyX/`HarmonyLib`, Unity IMGUI, xUnit, local generated ASKA interop DLLs.

**Spec:** `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`

**Research:** `docs/research/2026-09-14-aska-interop-observations.md`

## Global Constraints

- Complete Stage 1 first and preserve its public-repo dependency policy.
- Target ASKA build `25186770`, Unity `6000.3.12f1`, BepInEx `6.0.0-be.755`.
- Local/single-player only. `Unknown` or network/co-op session state MUST block gameplay cheats.
- Do not touch Steam achievement APIs.
- Do not introduce fixed memory offsets in v1.
- Do not guess game members. Local Codex MUST inspect the actual ignored local interop DLLs when a signature is not recorded in the research note.
- Every feature defaults off.
- A failing hook disables only its own feature and records a diagnostic reason.

---

### Task 1: Create local game-context adapters

**Files:**
- Create: `src/HutchASKA.Plugin/Game/IPlayerContext.cs`
- Create: `src/HutchASKA.Plugin/Game/IWorldContext.cs`
- Create: `src/HutchASKA.Plugin/Game/AskaPlayerContext.cs`
- Create: `src/HutchASKA.Plugin/Game/AskaWorldContext.cs`
- Create: `src/HutchASKA.Plugin/Game/GameObjectResolver.cs`
- Modify: `src/HutchASKA.Plugin/Plugin.cs`

**Interfaces:**
- Produces `IPlayerContext.TryGetLocalPlayer(out SSSGame.PlayerCharacter player)`.
- Produces `IWorldContext.TryGetWeatherSystem(out SSSGame.Weather.WeatherSystem weather)`.
- Later player/world features depend only on these adapters rather than performing their own scene searches.

- [ ] **Step 1: Inspect current local interop types before coding**

Use local tooling available to Codex (`dotnet`, reflection helper, ILSpy/dnSpy if installed, or a small throwaway reflection console app) to verify how the current build exposes the active `PlayerCharacter` and `WeatherSystem` singleton/instance. Record the exact findings by appending a dated subsection to `docs/research/2026-09-14-aska-interop-observations.md`.

Do not commit any proprietary DLL.

- [ ] **Step 2: Define narrow interfaces**

Use exact contracts:

```csharp
internal interface IPlayerContext
{
    bool TryGetLocalPlayer(out SSSGame.PlayerCharacter? player);
}

internal interface IWorldContext
{
    bool TryGetWeatherSystem(out SSSGame.Weather.WeatherSystem? weather);
}
```

- [ ] **Step 3: Implement resolver caching safely**

`GameObjectResolver` may cache a resolved object only while its Unity object remains valid. It must re-resolve after scene/load transitions rather than storing a stale unmanaged wrapper indefinitely.

- [ ] **Step 4: Build locally**

Run:

```powershell
.\scripts\Build-Local.ps1
```

Expected: plugin project builds against the current local interop assemblies.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Game src/HutchASKA.Plugin/Plugin.cs docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add ASKA player and world context adapters"
```

---

### Task 2: Implement player God Mode

**Files:**
- Create: `src/HutchASKA.Plugin/Player/GodModeFeature.cs`
- Create: `src/HutchASKA.Plugin/Player/Patches/PlayerDamagePatch.cs`
- Modify: `src/HutchASKA.Plugin/Plugin.cs`
- Test: `tests/HutchASKA.Core.Tests/Features/FeatureGuardTests.cs`

**Interfaces:**
- Consumes Stage 1 `ITrainerFeature`, registry, execution guard, and single-player decision.
- Targets verified `SSSGame.PlayerCharacter.TakeDamage(DamageData)` or the narrowest current local equivalent.

- [ ] **Step 1: Verify the exact `TakeDamage` overload locally**

Confirm namespace, declaring type, parameter type, and return type from the current `Assembly-CSharp.dll`. If both `Character.TakeDamage` and `PlayerCharacter.TakeDamage` exist, patch the narrowest player-specific call that covers local-player damage without suppressing villager damage.

- [ ] **Step 2: Add a core guard test**

Test that an enabled gameplay feature is blocked when `SinglePlayerDecision.Allowed == false`, and that its state becomes `Blocked` with the decision reason.

- [ ] **Step 3: Implement Harmony prefix**

The prefix must return `false` only when:

1. God Mode is enabled,
2. the affected instance is the resolved local player, and
3. the single-player guard is currently allowed.

Otherwise return `true` so native ASKA damage handling runs.

Do not alter `CurrentHealth` or `MaxHealth` as the God Mode mechanism.

- [ ] **Step 4: Run core tests and local build**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj
.\scripts\Build-Local.ps1
```

Expected: PASS and successful plugin build.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/Player tests/HutchASKA.Core.Tests/Features src/HutchASKA.Plugin/Plugin.cs
git commit -m "feat: add local-player god mode"
```

---

### Task 3: Implement infinite stamina

**Files:**
- Create: `src/HutchASKA.Plugin/Player/InfiniteStaminaFeature.cs`
- Create: `src/HutchASKA.Plugin/Player/Patches/StaminaDrainPatch.cs`

**Interfaces:**
- Target observed API: `SSSGame.Character.DrainStamina(float, DrainStaminaUsage)` and/or the verified local `CharacterMovement.DrainStamina` path.

- [ ] **Step 1: Verify the real stamina call path locally**

Inspect call targets and prefer the narrowest path that affects the local player only. Record the chosen method in the research note.

- [ ] **Step 2: Implement a prefix that suppresses local-player stamina drain only while enabled**

Do not continually assign an arbitrary stamina maximum if a drain suppression hook is sufficient.

- [ ] **Step 3: Verify disabled behavior**

After toggling the feature off in-game, sprint until stamina visibly drains. This is part of acceptance; merely compiling is insufficient.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Plugin/Player docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add infinite stamina"
```

---

### Task 4: Implement hunger, thirst, and temperature controls

**Files:**
- Create: `src/HutchASKA.Plugin/Player/SurvivalFeatureSet.cs`
- Create: `src/HutchASKA.Plugin/Player/VariableAttributeController.cs`
- Test: `tests/HutchASKA.Core.Tests/Player/NormalizedTargetTests.cs`

**Interfaces:**
- Verified player access: `PlayerCharacter.GetPlayerSurvival()`.
- Observed `CharacterSurvival`: `_foodVAttr`, `_waterVAttr`, `_warmthVAttr`.
- Verified `SandSailorStudio.Attributes.VariableAttribute`: `GetValue()`, `SetValue(float)`, `min`, `max`, `GetNormalizedValue()`.

- [ ] **Step 1: Add pure target-value helper tests**

Create a core helper that clamps a requested normalized fraction to `[0,1]` and maps it to `(min,max)` without assuming min is zero.

Test examples:

```csharp
Assert.Equal(100f, AttributeMath.ValueAtFraction(0f, 100f, 1f));
Assert.Equal(50f, AttributeMath.ValueAtFraction(0f, 100f, 0.5f));
Assert.Equal(20f, AttributeMath.ValueAtFraction(20f, 80f, 0f));
```

- [ ] **Step 2: Implement hunger and thirst maintenance**

While enabled, once per safe update tick set food/water to each attribute's actual `max`. Do not set `SuspendSurvival` for these features.

- [ ] **Step 3: Determine warmth semantics before implementing temperature immunity**

Inspect current values during a normal comfortable state and during a cold/hot state if practical. Choose a neutral/safe value from native game behavior rather than assuming `max` is always desirable.

If safe semantics cannot be confirmed, mark only Temperature Immunity `Incompatible` and continue with hunger/thirst.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Core src/HutchASKA.Plugin/Player tests/HutchASKA.Core.Tests/Player docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add player survival controls"
```

---

### Task 5: Implement movement-speed multiplier

**Files:**
- Create: `src/HutchASKA.Plugin/Player/MovementSpeedFeature.cs`
- Create: `src/HutchASKA.Core/Features/MultiplierSetting.cs`
- Test: `tests/HutchASKA.Core.Tests/Features/MultiplierSettingTests.cs`

**Interfaces:**
- `MultiplierSetting` exposes `float Value` clamped to configured min/max and a `Reset()` that restores `1.0f`.
- ASKA integration uses `PlayerCharacter.GetCharacterMovement()` and the exact local movement member verified from the current interop assembly.

- [ ] **Step 1: Test multiplier bounds**

Required assertions: values below 1 become 1, above 5 become 5, reset returns 1.

- [ ] **Step 2: Inspect `SSSGame.CharacterMovement` locally**

Identify a reversible speed multiplier or base/native value path. Do not hard-code a guessed field name from another ASKA version.

- [ ] **Step 3: Preserve the native baseline**

Capture the native value when applying the feature and restore that captured value at `1.0x`/disable. Avoid assuming the native underlying value is numerically `1` unless the API itself is a multiplier.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Core/Features src/HutchASKA.Plugin/Player tests/HutchASKA.Core.Tests/Features docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add player movement multiplier"
```

---

### Task 6: Implement world time and game speed

**Files:**
- Create: `src/HutchASKA.Plugin/World/WorldTimeFeature.cs`
- Create: `src/HutchASKA.Plugin/World/GameSpeedFeature.cs`
- Create: `src/HutchASKA.Core/World/TimeAdjustment.cs`
- Test: `tests/HutchASKA.Core.Tests/World/TimeAdjustmentTests.cs`

**Interfaces:**
- Verified `SSSGame.Weather.WeatherSystem`: `TimeRunningEnabled`, `TimeSpeedMultiplier`, `SetGameTime(float)`, `ToggleTimePassing()`.

- [ ] **Step 1: Determine the units/range of `SetGameTime(float)` locally**

Use reflection and/or runtime observation. Record whether the float represents normalized day fraction, hours, seconds, or another native unit. Do not implement `+1h/-1h` until this is confirmed.

- [ ] **Step 2: Add wraparound tests for the pure time helper**

If the native unit is hours, test `23.5 + 1 => 0.5` and `0.5 - 1 => 23.5`. If the native unit differs, encode equivalent exact tests after confirming semantics.

- [ ] **Step 3: Implement Freeze Time**

Prefer assigning `TimeRunningEnabled = false/true` if it is a normal mutable property. Preserve the original state when the feature first takes control and restore it on disable/reset.

- [ ] **Step 4: Implement ±1 hour**

Use `SetGameTime(float)` with the confirmed native representation and wrap correctly.

- [ ] **Step 5: Implement game-speed presets**

First preference is ASKA's `WeatherSystem.TimeSpeedMultiplier` when it provides the desired gameplay-time acceleration without breaking UI/input. If the intended WeMod-style global game speed requires Unity `Time.timeScale`, keep it as a separate verified path and preserve the original value. Do not conflate movement speed with game speed.

- [ ] **Step 6: Commit**

```bash
git add src/HutchASKA.Core/World src/HutchASKA.Plugin/World tests/HutchASKA.Core.Tests/World docs/research/2026-09-14-aska-interop-observations.md
git commit -m "feat: add world time and game speed controls"
```

---

### Task 7: Build the F8 Player/World UI and hotkey routing

**Files:**
- Create: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/PlayerTab.cs`
- Create: `src/HutchASKA.Plugin/UI/Tabs/WorldTab.cs`
- Create: `src/HutchASKA.Plugin/Input/HotkeyManager.cs`
- Modify: `src/HutchASKA.Plugin/Plugin.cs`

**Interfaces:**
- UI reads/writes feature state only through the feature registry and feature-specific public settings.
- Default keys: `F1` God Mode, `F2` Infinite Stamina, `F5` Freeze Time, `F8` show/hide UI.

- [ ] **Step 1: Implement F8 toggle and cursor/input behavior**

When the window opens, preserve prior cursor lock/visibility state and make the cursor usable. When it closes, restore prior state. Avoid globally swallowing unrelated input when the UI is hidden.

- [ ] **Step 2: Implement Player tab**

Required controls: God Mode, Infinite Stamina, Infinite Hunger, Infinite Thirst, Temperature Immunity, Movement Speed 1.0x–5.0x.

Unavailable/incompatible features must be visibly disabled with their diagnostic reason.

- [ ] **Step 3: Implement World tab**

Required controls: Freeze Time, -1 Hour, +1 Hour, game-speed presets 0.5x/1x/2x/5x.

- [ ] **Step 4: Implement hotkey routing**

Hotkeys must not toggle blocked/incompatible features. F8 remains available even when gameplay features are blocked so diagnostics can be viewed.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Plugin/UI src/HutchASKA.Plugin/Input src/HutchASKA.Plugin/Plugin.cs
git commit -m "feat: add player and world trainer UI"
```

---

### Task 8: Stage 2 local smoke test and documentation

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Create: `docs/testing/stage-2-smoke-test.md`

- [ ] **Step 1: Build and install**

```powershell
.\scripts\Build-Local.ps1
.\scripts\Install-Local.ps1
```

- [ ] **Step 2: Run the exact smoke matrix**

In a disposable/safely backed-up single-player save verify:

1. F8 opens/closes the trainer.
2. God Mode blocks damage; disabling restores damage.
3. Infinite Stamina blocks drain; disabling restores drain.
4. Hunger and thirst hold at native maxima independently.
5. Temperature toggle behaves safely or reports `Incompatible` without breaking other features.
6. Movement 2x is visibly faster; resetting to 1x restores native movement.
7. Freeze Time halts world clock only and resumes correctly.
8. +1/-1 hour works across day boundaries.
9. Each game-speed preset restores correctly to 1x.
10. Incompatible feature simulation does not crash the plugin.
11. BepInEx log contains no repeating exception loop.

- [ ] **Step 3: Update README and changelog**

Document the working controls and mark unverified/disabled features accurately. Do not claim a feature works unless it passed the smoke matrix.

- [ ] **Step 4: Run final verification**

```powershell
dotnet test .\tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj
.\scripts\Build-Local.ps1
git status --short
git ls-files '*.dll' '*.exe'
```

Expected: tests PASS, local plugin builds, and no proprietary/game binary is tracked.

- [ ] **Step 5: Commit**

```bash
git add README.md CHANGELOG.md docs/testing/stage-2-smoke-test.md
git commit -m "docs: verify player and world trainer stage"
```

## Stage 2 Exit Criteria

Stage 2 is complete only when the local plugin loads in the current ASKA build, F8 works, at least God Mode/stamina/hunger/thirst/time controls pass in-game verification, any unsupported control fails closed, all core tests pass, and no game DLL is tracked in Git.
