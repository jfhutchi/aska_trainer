# HutchASKA Stage 1: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a public, legally clean, testable HutchASKA repository foundation with a pure .NET core, a BepInEx IL2CPP plugin shell, local-only ASKA dependency resolution, configuration/state infrastructure, diagnostics, and release-quality documentation.

**Architecture:** Split code into `HutchASKA.Core` (no proprietary/game dependencies; fully testable in Codex/GitHub Actions) and `HutchASKA.Plugin` (BepInEx/Unity/ASKA integration compiled locally against the user's generated interop assemblies). The plugin consumes core state models and fails closed when ASKA references/session state are unavailable.

**Tech Stack:** C# / .NET 6 target, BepInEx 6 IL2CPP, HarmonyX/`HarmonyLib` from the local BepInEx installation, Unity IMGUI for the trainer shell, xUnit for pure-core tests, PowerShell for local build/install helpers, GitHub Actions using Node 24-compatible official actions.

**Spec:** `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`

**Research:** `docs/research/2026-09-14-aska-interop-observations.md`

## Global Constraints

- Initial compatibility target: ASKA Steam build `25186770`.
- Unity target: `6000.3.12f1`.
- BepInEx target: `6.0.0-be.755`, IL2CPP.
- Runtime compatibility target: `.NET 6.0.7` / `net6.0`.
- Windows, local/single-player only. Unknown session state MUST be blocked.
- Do not patch or call Steam achievement APIs.
- Do not commit `Assembly-CSharp.dll`, `SandSailorStudio.dll`, generated interop DLLs, ASKA assets, or other proprietary game binaries.
- Do not copy WeMod code or attempt to bypass WeMod restrictions.
- Prefer direct verified ASKA APIs, then narrow Harmony patches; do not introduce fixed memory offsets in v1.
- Every feature is off by default on first install.
- A single feature failure must not crash or disable unrelated features.
- GitHub Actions must use Node 24-compatible action majors (`actions/checkout@v6`, `actions/setup-dotnet@v6` or newer Node 24-compatible majors if those are superseded during implementation).
- Repository license: MIT, for HutchASKA-authored source only. Use `Copyright (c) 2026 jfhutchi` rather than inventing a legal name.

---

## Locked File Structure

```text
aska_trainer/
├── .github/
│   └── workflows/
│       └── core-ci.yml
├── docs/
│   ├── research/
│   └── superpowers/
│       ├── plans/
│       └── specs/
├── libs/
│   └── README.md
├── scripts/
│   ├── Build-Local.ps1
│   └── Install-Local.ps1
├── src/
│   ├── HutchASKA.Core/
│   │   ├── Compatibility/
│   │   ├── Configuration/
│   │   ├── Features/
│   │   └── Input/
│   └── HutchASKA.Plugin/
│       ├── Game/
│       ├── Infrastructure/
│       ├── UI/
│       └── Plugin.cs
├── tests/
│   └── HutchASKA.Core.Tests/
├── .gitignore
├── CHANGELOG.md
├── Directory.Build.props
├── HutchASKA.sln
├── LICENSE
└── README.md
```

`HutchASKA.Core` MUST NOT reference BepInEx, Unity, Harmony, `Assembly-CSharp`, or `SandSailorStudio`. This boundary is what makes Codex and public CI useful without distributing game DLLs.

---

### Task 1: Legal, README, repository hygiene, and dependency documentation

**Files:**
- Create: `LICENSE`
- Create: `README.md`
- Create: `CHANGELOG.md`
- Create: `.gitignore`
- Create: `libs/README.md`

**Interfaces:**
- Consumes: approved design spec and interop research notes.
- Produces: public-facing license/install/build/compatibility contract used by every later task.

- [ ] **Step 1: Create the MIT license exactly for repository-authored code**

Create `LICENSE` with the standard MIT text and this copyright line:

```text
MIT License

Copyright (c) 2026 jfhutchi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

- [ ] **Step 2: Create a complete root README**

`README.md` must contain these sections in this order:

```markdown
# HutchASKA

HutchASKA is a free, open-source, single-player in-game trainer for the Steam game ASKA, implemented as a BepInEx 6 IL2CPP plugin.

> HutchASKA is an unofficial community project. It is not affiliated with or endorsed by Sand Sailor Studio, Thunderful, Steam, Valve, or WeMod.

## Status
## Compatibility
## Features
## Requirements
## Install a Release
## Build From Source
## Local ASKA References
## Controls
## Single-Player Safety
## Steam Achievements
## Updating After an ASKA Patch
## Diagnostics and Logs
## Known Limitations
## Contributing
## License
## Third-Party / Game Assets
```

README requirements:

- Compatibility table names ASKA build `25186770`, Unity `6000.3.12f1`, and BepInEx `6.0.0-be.755` as the initial tested target.
- State clearly that no ASKA/BepInEx proprietary/generated binaries are included.
- Explain that the plugin is single-player-only and blocks gameplay features when local session state is not confirmed.
- Explain that HutchASKA does not touch Steam achievement APIs, while cheats can make normal achievements easier.
- Include release install destination: `ASKA\BepInEx\plugins\HutchASKA\HutchASKA.Plugin.dll`.
- Include local build environment variable example:

```powershell
$env:ASKA_GAME_DIR = "C:\Program Files (x86)\Steam\steamapps\common\ASKA"
.\scripts\Build-Local.ps1
```

- Include the first-run BepInEx requirement: launch ASKA once after BepInEx installation so `BepInEx\interop` exists.
- Include the ASKA update recovery instruction: regenerate interop when necessary, then rebuild/retest HutchASKA rather than copying stale DLLs into Git.

- [ ] **Step 3: Create `libs/README.md` with the local reference contract**

Document these expected local files without copying them into the repository:

```text
%ASKA_GAME_DIR%\BepInEx\core\BepInEx.Core.dll
%ASKA_GAME_DIR%\BepInEx\core\BepInEx.Unity.IL2CPP.dll
%ASKA_GAME_DIR%\BepInEx\core\0Harmony.dll
%ASKA_GAME_DIR%\BepInEx\core\Il2CppInterop.Runtime.dll
%ASKA_GAME_DIR%\BepInEx\interop\Assembly-CSharp.dll
%ASKA_GAME_DIR%\BepInEx\interop\SandSailorStudio.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.CoreModule.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.IMGUIModule.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.InputLegacyModule.dll
```

If a local BepInEx distribution uses a different exact core filename, the build script must report that missing file rather than silently substituting an unrelated assembly.

- [ ] **Step 4: Create `.gitignore`**

At minimum ignore:

```gitignore
bin/
obj/
.vs/
.idea/
*.user
*.suo
TestResults/
artifacts/
libs/local/
**/BepInEx/interop/
**/Assembly-CSharp.dll
**/SandSailorStudio.dll
*.pdb
.env
```

Do NOT blanket-ignore all DLLs because future release metadata or non-proprietary test fixtures may need explicit handling.

- [ ] **Step 5: Create `CHANGELOG.md`**

Use Keep-a-Changelog-style headings with an initial:

```markdown
# Changelog

## [Unreleased]

### Added
- Project design and implementation planning.
- MIT licensing and public repository documentation.
```

- [ ] **Step 6: Verify repository hygiene**

Run:

```bash
git status --short
git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'
```

Expected: the second command prints nothing.

- [ ] **Step 7: Commit**

```bash
git add LICENSE README.md CHANGELOG.md .gitignore libs/README.md
git commit -m "docs: add project readme license and dependency policy"
```

---

### Task 2: Create solution, pure core project, and test project

**Files:**
- Create: `HutchASKA.sln`
- Create: `Directory.Build.props`
- Create: `src/HutchASKA.Core/HutchASKA.Core.csproj`
- Create: `tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj`
- Create: `tests/HutchASKA.Core.Tests/SmokeTests.cs`

**Interfaces:**
- Consumes: no game APIs.
- Produces: a `net6.0` core library and xUnit test assembly used by all subsequent plans.

- [ ] **Step 1: Scaffold the solution and projects**

Run:

```bash
dotnet new sln -n HutchASKA
dotnet new classlib -n HutchASKA.Core -o src/HutchASKA.Core --framework net6.0
dotnet new xunit -n HutchASKA.Core.Tests -o tests/HutchASKA.Core.Tests --framework net6.0
dotnet sln HutchASKA.sln add src/HutchASKA.Core/HutchASKA.Core.csproj
dotnet sln HutchASKA.sln add tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
dotnet add tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj reference src/HutchASKA.Core/HutchASKA.Core.csproj
```

Delete template `Class1.cs` and `UnitTest1.cs`.

- [ ] **Step 2: Add common compiler settings**

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Write the failing smoke test**

Create `tests/HutchASKA.Core.Tests/SmokeTests.cs`:

```csharp
namespace HutchASKA.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssembly_HasExpectedName()
    {
        Assert.Equal("HutchASKA.Core", typeof(CoreMarker).Assembly.GetName().Name);
    }
}
```

Expected initial failure: `CoreMarker` does not exist.

- [ ] **Step 4: Run the test to verify failure**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected: compile failure mentioning `CoreMarker`.

- [ ] **Step 5: Add the minimal marker**

Create `src/HutchASKA.Core/CoreMarker.cs`:

```csharp
namespace HutchASKA.Core;

public static class CoreMarker
{
}
```

- [ ] **Step 6: Run all core tests**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add HutchASKA.sln Directory.Build.props src/HutchASKA.Core tests/HutchASKA.Core.Tests
git commit -m "build: scaffold testable trainer core"
```

---

### Task 3: Implement feature lifecycle and registry

**Files:**
- Create: `src/HutchASKA.Core/Features/FeatureState.cs`
- Create: `src/HutchASKA.Core/Features/CompatibilityResult.cs`
- Create: `src/HutchASKA.Core/Features/ITrainerFeature.cs`
- Create: `src/HutchASKA.Core/Features/FeatureRegistry.cs`
- Test: `tests/HutchASKA.Core.Tests/Features/FeatureRegistryTests.cs`

**Interfaces:**
- Produces: `FeatureState`, `CompatibilityResult`, `ITrainerFeature`, `FeatureRegistry`.
- Later plugin modules register one `ITrainerFeature` per trainer feature and the diagnostics UI reads the registry.

- [ ] **Step 1: Write registry tests first**

Create tests covering unique IDs, lookup, and states:

```csharp
[Fact]
public void Register_RejectsDuplicateFeatureId()
{
    var registry = new FeatureRegistry();
    registry.Register(new FakeFeature("god-mode"));

    var ex = Assert.Throws<InvalidOperationException>(() =>
        registry.Register(new FakeFeature("god-mode")));

    Assert.Contains("god-mode", ex.Message);
}

[Fact]
public void Snapshot_ReturnsFeaturesInRegistrationOrder()
{
    var registry = new FeatureRegistry();
    registry.Register(new FakeFeature("god-mode"));
    registry.Register(new FakeFeature("stamina"));

    Assert.Equal(new[] { "god-mode", "stamina" }, registry.Snapshot().Select(x => x.Id));
}
```

The test file must contain a minimal local `FakeFeature : ITrainerFeature`.

- [ ] **Step 2: Run tests and verify failure**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj --filter FeatureRegistryTests
```

Expected: missing feature types.

- [ ] **Step 3: Implement the feature contracts**

Use these exact shapes:

```csharp
namespace HutchASKA.Core.Features;

public enum FeatureState
{
    Disabled,
    Enabled,
    Blocked,
    Incompatible,
    Faulted
}

public sealed record CompatibilityResult(bool IsCompatible, string? Reason)
{
    public static CompatibilityResult Compatible() => new(true, null);
    public static CompatibilityResult Incompatible(string reason) => new(false, reason);
}

public interface ITrainerFeature
{
    string Id { get; }
    string DisplayName { get; }
    FeatureState State { get; }
    string? StatusReason { get; }
    CompatibilityResult ProbeCompatibility();
    bool TryEnable();
    void Disable();
    void Reset();
    void Tick();
}
```

`FeatureRegistry` must use a case-insensitive ID dictionary and preserve registration order.

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Core/Features tests/HutchASKA.Core.Tests/Features
git commit -m "feat: add trainer feature lifecycle registry"
```

---

### Task 4: Add fault isolation and circuit breaker

**Files:**
- Create: `src/HutchASKA.Core/Compatibility/FeatureCircuitBreaker.cs`
- Create: `src/HutchASKA.Core/Compatibility/FeatureExecutionGuard.cs`
- Test: `tests/HutchASKA.Core.Tests/Compatibility/FeatureCircuitBreakerTests.cs`

**Interfaces:**
- Produces: `FeatureCircuitBreaker.RecordFailure(Exception)`, `IsOpen`, `LastError`; `FeatureExecutionGuard.TryRun(...)`.
- Later feature implementations wrap patch/tick operations through this guard.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void CircuitBreaker_OpensAfterThreeFailures()
{
    var breaker = new FeatureCircuitBreaker(3);
    breaker.RecordFailure(new InvalidOperationException("one"));
    breaker.RecordFailure(new InvalidOperationException("two"));
    Assert.False(breaker.IsOpen);
    breaker.RecordFailure(new InvalidOperationException("three"));
    Assert.True(breaker.IsOpen);
    Assert.Equal("three", breaker.LastError?.Message);
}

[Fact]
public void SuccessfulExecution_DoesNotIncrementFailures()
{
    var breaker = new FeatureCircuitBreaker(3);
    var guard = new FeatureExecutionGuard(breaker);
    Assert.True(guard.TryRun(() => { }));
    Assert.Equal(0, breaker.FailureCount);
}
```

- [ ] **Step 2: Verify tests fail**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj --filter "FeatureCircuitBreakerTests"
```

- [ ] **Step 3: Implement minimal breaker/guard**

`FeatureCircuitBreaker` must reject thresholds less than 1, store only the most recent exception, and never throw from `RecordFailure`.

`FeatureExecutionGuard.TryRun(Action)` returns false when the breaker is already open or the action throws; it records thrown exceptions.

- [ ] **Step 4: Run all tests**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/HutchASKA.Core/Compatibility tests/HutchASKA.Core.Tests/Compatibility
git commit -m "feat: isolate feature runtime failures"
```

---

### Task 5: Add settings and hotkey models

**Files:**
- Create: `src/HutchASKA.Core/Configuration/TrainerSettings.cs`
- Create: `src/HutchASKA.Core/Input/TrainerHotkey.cs`
- Create: `src/HutchASKA.Core/Input/HotkeyMap.cs`
- Test: `tests/HutchASKA.Core.Tests/Configuration/TrainerSettingsTests.cs`
- Test: `tests/HutchASKA.Core.Tests/Input/HotkeyMapTests.cs`

**Interfaces:**
- Produces: game-independent defaults and key names consumed by the BepInEx config adapter.

- [ ] **Step 1: Write default-setting tests**

```csharp
[Fact]
public void Defaults_AreSafeAndDisabled()
{
    var settings = TrainerSettings.CreateDefaults();
    Assert.False(settings.RestoreEnabledStatesOnLaunch);
    Assert.Equal(1.0f, settings.MovementSpeedMultiplier);
    Assert.Equal(1.0f, settings.GameSpeedMultiplier);
    Assert.Equal("F8", settings.MenuHotkey);
}

[Theory]
[InlineData(0.1f, 1.0f)]
[InlineData(3.0f, 3.0f)]
[InlineData(10.0f, 5.0f)]
public void ClampMovementMultiplier_UsesOneToFiveRange(float input, float expected)
{
    Assert.Equal(expected, TrainerSettings.ClampMovementMultiplier(input));
}
```

- [ ] **Step 2: Write hotkey conflict test**

```csharp
[Fact]
public void Set_RejectsDuplicateNonEmptyHotkey()
{
    var map = HotkeyMap.CreateDefaults();
    var ex = Assert.Throws<InvalidOperationException>(() => map.Set("stamina", "F1"));
    Assert.Contains("F1", ex.Message);
}
```

Defaults:

```text
menu = F8
god-mode = F1
stamina = F2
freeze-time = F5
```

- [ ] **Step 3: Verify tests fail, implement, rerun**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected after implementation: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/HutchASKA.Core/Configuration src/HutchASKA.Core/Input tests/HutchASKA.Core.Tests/Configuration tests/HutchASKA.Core.Tests/Input
git commit -m "feat: add safe trainer settings and hotkeys"
```

---

### Task 6: Create local-only plugin project and dependency validation

**Files:**
- Create: `src/HutchASKA.Plugin/HutchASKA.Plugin.csproj`
- Create: `scripts/Build-Local.ps1`
- Create: `scripts/Install-Local.ps1`
- Modify: `HutchASKA.sln`

**Interfaces:**
- Consumes: `%ASKA_GAME_DIR%` and `HutchASKA.Core`.
- Produces: local `HutchASKA.Plugin.dll`; no game references are copied to output.

- [ ] **Step 1: Add plugin project with explicit local-reference gate**

The project targets `net6.0`, references `HutchASKA.Core`, and resolves local references from `$(ASKA_GAME_DIR)`. Use MSBuild conditions so missing references produce this exact actionable error:

```text
ASKA references not found. Set ASKA_GAME_DIR to the ASKA folder containing ASKA.exe, launch ASKA once with BepInEx so BepInEx\interop exists, then build again.
```

Set `<Private>false</Private>` on every BepInEx/Unity/ASKA assembly reference so proprietary/runtime assemblies are not copied beside the plugin.

- [ ] **Step 2: Add the plugin project to the solution**

```bash
dotnet sln HutchASKA.sln add src/HutchASKA.Plugin/HutchASKA.Plugin.csproj
```

- [ ] **Step 3: Add `Build-Local.ps1`**

The script must:

1. accept optional `-AskaGameDir` and otherwise use `$env:ASKA_GAME_DIR`;
2. validate `ASKA.exe`, `BepInEx\core`, and `BepInEx\interop\Assembly-CSharp.dll`;
3. set `ASKA_GAME_DIR` for the build process;
4. run `dotnet build src\HutchASKA.Plugin\HutchASKA.Plugin.csproj -c Release`;
5. exit non-zero on failure.

Use:

```powershell
param([string]$AskaGameDir = $env:ASKA_GAME_DIR)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AskaGameDir)) {
    throw 'ASKA_GAME_DIR is not set. Pass -AskaGameDir or set the environment variable.'
}
$resolved = (Resolve-Path $AskaGameDir).Path
$required = @(
    (Join-Path $resolved 'ASKA.exe'),
    (Join-Path $resolved 'BepInEx\core'),
    (Join-Path $resolved 'BepInEx\interop\Assembly-CSharp.dll'),
    (Join-Path $resolved 'BepInEx\interop\SandSailorStudio.dll')
)
foreach ($path in $required) {
    if (-not (Test-Path $path)) { throw "Required ASKA/BepInEx path not found: $path" }
}
$env:ASKA_GAME_DIR = $resolved
dotnet build 'src\HutchASKA.Plugin\HutchASKA.Plugin.csproj' -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

- [ ] **Step 4: Add `Install-Local.ps1`**

It invokes `Build-Local.ps1`, creates `BepInEx\plugins\HutchASKA`, and copies only `HutchASKA.Plugin.dll` plus any HutchASKA-authored companion DLL (for example `HutchASKA.Core.dll`) into that folder. It MUST NOT copy game/BepInEx DLLs.

- [ ] **Step 5: Validate cloud-safe and local-missing behavior**

Without `ASKA_GAME_DIR`:

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
dotnet build src/HutchASKA.Plugin/HutchASKA.Plugin.csproj -c Release
```

Expected: tests PASS; plugin build FAILS with the exact actionable missing-reference message.

- [ ] **Step 6: Commit**

```bash
git add HutchASKA.sln src/HutchASKA.Plugin scripts
git commit -m "build: add local ASKA plugin build pipeline"
```

---

### Task 7: Add plugin bootstrap, single-player gate contract, and diagnostics shell

**Files:**
- Create: `src/HutchASKA.Core/Compatibility/SessionMode.cs`
- Create: `src/HutchASKA.Core/Compatibility/SinglePlayerDecision.cs`
- Create: `src/HutchASKA.Plugin/Plugin.cs`
- Create: `src/HutchASKA.Plugin/Infrastructure/SinglePlayerGuard.cs`
- Create: `src/HutchASKA.Plugin/Infrastructure/FeatureHost.cs`
- Create: `src/HutchASKA.Plugin/UI/TrainerBehaviour.cs`
- Create: `src/HutchASKA.Plugin/UI/TrainerWindow.cs`
- Create: `src/HutchASKA.Plugin/UI/DiagnosticsPanel.cs`
- Test: `tests/HutchASKA.Core.Tests/Compatibility/SinglePlayerDecisionTests.cs`

**Interfaces:**
- Produces: a BepInEx plugin that opens an empty tabbed trainer shell with F8 and a Diagnostics tab.
- `SinglePlayerDecision.Evaluate(SessionMode)` is pure/testable; game session discovery remains in the plugin adapter.

- [ ] **Step 1: Write fail-closed session tests**

```csharp
[Theory]
[InlineData(SessionMode.SinglePlayer, true, null)]
[InlineData(SessionMode.Multiplayer, false, "Multiplayer/co-op session detected")]
[InlineData(SessionMode.Unknown, false, "Single-player state not confirmed")]
public void Evaluate_FailsClosed(SessionMode mode, bool allowed, string? reason)
{
    var result = SinglePlayerDecision.Evaluate(mode);
    Assert.Equal(allowed, result.Allowed);
    Assert.Equal(reason, result.Reason);
}
```

- [ ] **Step 2: Implement the pure gate**

```csharp
public enum SessionMode { Unknown, SinglePlayer, Multiplayer }

public sealed record SinglePlayerDecision(bool Allowed, string? Reason)
{
    public static SinglePlayerDecision Evaluate(SessionMode mode) => mode switch
    {
        SessionMode.SinglePlayer => new(true, null),
        SessionMode.Multiplayer => new(false, "Multiplayer/co-op session detected"),
        _ => new(false, "Single-player state not confirmed")
    };
}
```

- [ ] **Step 3: Implement the BepInEx plugin entry point**

Use the normal BepInEx IL2CPP shape:

```csharp
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "com.jfhutchi.hutchaska";
    public const string PluginName = "HutchASKA";
    public const string PluginVersion = "0.1.0";

    public override void Load()
    {
        // Initialize config/logging, register TrainerBehaviour with IL2CPP,
        // create a persistent GameObject, and attach the behaviour.
    }
}
```

Do not add an `AskaUI` dependency. Use Unity IMGUI so HutchASKA is self-contained.

- [ ] **Step 4: Implement the UI shell**

The F8 window contains these tabs even before features are implemented:

```text
Player | Items | Crafting & Building | World | Tribe | Advanced | Diagnostics
```

When open, show version and current session-gate state. Do not enable gameplay toggles in Stage 1.

- [ ] **Step 5: Implement diagnostics model rendering**

The Diagnostics tab lists registered feature ID, display name, state, and status reason. With no gameplay features yet, it must still show plugin version, Unity/BepInEx information where available, and the single-player decision.

- [ ] **Step 6: Run core tests**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 7: Local verification requirement**

On the user's Windows machine with `ASKA_GAME_DIR` configured:

```powershell
.\scripts\Install-Local.ps1
```

Expected after launching ASKA:

```text
BepInEx loads HutchASKA 0.1.0
F8 opens/closes the HutchASKA window
Diagnostics renders without exceptions
No gameplay-changing feature is enabled
```

If Codex cannot perform this local step, it MUST leave a concise `LOCAL_VERIFICATION_REQUIRED` note in its final summary rather than claiming runtime success.

- [ ] **Step 8: Commit**

```bash
git add src/HutchASKA.Core/Compatibility src/HutchASKA.Plugin tests/HutchASKA.Core.Tests/Compatibility
git commit -m "feat: add trainer bootstrap and diagnostics shell"
```

---

### Task 8: Add public CI for the cloud-safe core

**Files:**
- Create: `.github/workflows/core-ci.yml`
- Modify: `README.md`

**Interfaces:**
- Produces: automated restore/build/test for code that does not need proprietary game assemblies.

- [ ] **Step 1: Add Node 24-compatible workflow**

Use:

```yaml
name: Core CI

on:
  push:
    branches: [main]
  pull_request:

permissions:
  contents: read

jobs:
  test-core:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '8.0.x'
      - name: Restore and test net6 core
        run: dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj --configuration Release
```

The .NET 8 SDK may build `net6.0` target projects; do not change the target framework merely because the CI SDK is newer.

- [ ] **Step 2: Document CI scope in README**

State explicitly that public CI validates the pure core. The actual ASKA plugin build is local because the repository intentionally does not redistribute ASKA-generated interop binaries.

- [ ] **Step 3: Run core tests locally/in Codex**

```bash
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj --configuration Release
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/core-ci.yml README.md
git commit -m "ci: test cloud-safe trainer core"
```

---

## Stage 1 Acceptance Gate

Before moving to Stage 2, all of the following must be true:

```text
[ ] MIT LICENSE exists and GitHub can detect it after indexing.
[ ] README contains install, build, compatibility, safety, achievements, update, and license sections.
[ ] No proprietary game DLL is tracked by Git.
[ ] HutchASKA.Core builds and all xUnit tests pass in Codex/CI.
[ ] Missing ASKA_GAME_DIR produces an actionable plugin-build error.
[ ] Local build/install scripts never copy game/BepInEx assemblies.
[ ] F8 trainer shell and Diagnostics work in a local ASKA runtime test, or the implementation is explicitly marked as requiring that local verification.
[ ] Unknown session state blocks gameplay features.
```

## Stage 1 Final Verification

Run in Codex/CI:

```bash
git status --short
git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'
dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj --configuration Release
```

Expected: clean tree after commits; no proprietary binaries listed; tests PASS.

Run on the user's ASKA machine:

```powershell
$env:ASKA_GAME_DIR = '<ASKA installation folder>'
.\scripts\Install-Local.ps1
```

Then launch ASKA and verify F8/Diagnostics before Stage 2 gameplay patches are introduced.
