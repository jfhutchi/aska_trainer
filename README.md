# HutchASKA

HutchASKA is a free, open-source, single-player in-game trainer for the Steam game ASKA, implemented as a BepInEx 6 IL2CPP plugin.

> HutchASKA is an unofficial community project. It is not affiliated with or endorsed by Sand Sailor Studio, Thunderful, Steam, Valve, or WeMod.

## Status

**Version `0.1.1` is a development candidate, not a gameplay-validated v1 release.** All four implementation stages are represented in source, including explicit Incompatible controls where safe native hooks could not be established. There are 66 passing core tests and a successful local plugin build. Version 0.1.0 startup was observed, but opening its menu exposed stripped Unity toolbar helpers. Version 0.1.1 removes those calls and contains rendering faults; an in-game retest remains **MANUAL VERIFICATION REQUIRED**. See the [GUI repair evidence](docs/testing/gui-rendering-repair.md), [release checklist](docs/testing/v1-release-checklist.md), [implementation ledger](docs/testing/implementation-status.md) and [loader evidence](docs/testing/local-loader-check.md). All cheats start off on first installation; automatic restoration of enabled states defaults to off.

Source projects and local build/install helpers are available. No validated release archive has been published. An in-game smoke test is required before a release can claim runtime compatibility.

## Compatibility

These versions were detected in the local Steam manifest, installed assembly metadata and fresh startup logs on 2026-09-14. The initial target remains the installed build. They are **not a tested gameplay release matrix**; startup does not verify behavior in a save.

| Component | Detected version | Evidence |
| --- | --- | --- |
| ASKA Steam build | `25186770` | Detected in installed Steam manifest |
| Full game version | `1.43.0809261352._PC.Release` | Detected in fresh Unity log |
| Application.version | `0.4` | Separate application value, not the Steam build ID |
| Unity | `6000.3.12f1` | Detected at plugin startup |
| BepInEx | `6.0.0-be.755`, IL2CPP | Commit `3fab71a1914132a1ce3a545caf3192da603f2258`, detected at startup |
| Runtime / target framework | `.NET 6.0.7` / `net6.0` | Runtime detected in startup log; initial compile target |
| Platform / session | Windows, local single-player | Multiplayer/co-op unsupported |

Installed HarmonyX is `2.10.2`; Il2CppInterop is `1.5.1-ci.829`, commit `6d9007c18cc8440830379c5e1d5714085e7ec577`. Local development uses .NET SDK `8.0.423`, test runtime `6.0.36` and PowerShell `7.6.5`.

Future release notes must state the exact ASKA, Unity, and BepInEx versions tested and any incompatible features. A game update may invalidate individual hooks even if the plugin still loads.

## Features

Implemented controls compile against current local interop signatures and are **not yet verified in a save**. Incompatible controls are visible, disabled and explain their reason. All gameplay toggles default off.

| Area | Controls and implementation status |
| --- | --- |
| Player | Implemented: God Mode, infinite stamina, independent hunger/thirst, movement 1.0x-5.0x. Temperature Immunity: Incompatible, safe warmth range unverified |
| Items | Implemented: runtime catalog/search and native Give Item/Give Stack; save persistence pending. Durability, freshness and retention: Incompatible, isolated native loss/consumption unverified |
| Crafting & Building | Free crafting, construction and repairs: Incompatible, narrow native transactions unverified |
| World | Implemented: Freeze Time and separate game-speed presets 0.5x/1x/2x/5x. -1/+1 hour: Incompatible, SetGameTime units/day boundaries unverified |
| Tribe | Implemented: current owned-villager damage suppression; independent food/water/energy/rest/happiness maintenance; Heal Entire Tribe; Restore All Needs. Temperature/Freeze Aging: Incompatible, safe warmth/lifetime behavior unverified |
| Villagers | Implemented: name/ID search, fresh-ID resolution, changed-field-only health/food/water/energy/rest/happiness edits, Heal/Max Needs/Apply. Warmth is read-only; age is unavailable. Instant normal recruitment: Incompatible, timer completion/rearm unverified |
| Advanced & Diagnostics | Implemented: Reset All, config reload, compatibility rescan, logging verbosity, optional state persistence, configurable hotkeys, actual Steam build detection, feature states and error reasons |

God Mode must block damage without inflating maximum health. Durability and freshness toggles preserve existing values. Add Items On Use retains quantity instead of duplicating arbitrary items. Recruitment must complete ASKA's normal lifecycle; arbitrary villager spawning is out of scope. Blueprint requirement bypass is optional and depends on a verified narrow hook.

The durability, freshness and retention sentences describe the required behavior; those hooks are currently Incompatible. Give uses ASKA's native definition-based insertion rather than raw item construction, checks ownership/capacity, limits quantities to 1-999 and measures the amount actually added. Initialization and save persistence still need acceptance. Tribe maintenance resolves current registered, living, locally owned members every half second and retains no raw villagers across calls. Guests and ambiguous IDs are excluded. Max Needs leaves warmth and lifetime untouched. Remaining lifetime is never presented as chronological age. No recruitment spawn call, constructor or speculative timer write is installed.

## Requirements

- A local Windows installation of ASKA acquired separately through Steam.
- A compatible BepInEx 6 IL2CPP installation acquired separately. No ASKA binaries, BepInEx runtime bundle, or generated interop binaries are included here.
- A first ASKA launch after installing BepInEx, allowing generation of `BepInEx\interop`; close the game before building or installing HutchASKA.
- For source builds and packaging: PowerShell 7, Git, and the .NET 8 SDK (C# 12 source, targeting `net6.0`). Running the core tests also requires a compatible .NET 6 runtime.

Follow the [official BepInEx IL2CPP installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html) for loader setup. Confirm a fresh chainloader log before installing the trainer. This machine initially needed the local Doorstop setting `ignore_disable_switch=true` to resolve an inherited disable condition; its original configuration was preserved outside Git. The HutchASKA installer does not change Doorstop. See the [loader record](docs/testing/local-loader-check.md).

## Install a Release

No validated release is published. For an explicitly selected local development candidate:

1. Read its compatibility matrix and known limitations. Back up the save you intend to use.
2. Install the specified BepInEx IL2CPP build separately, launch ASKA once to generate interop, then exit ASKA.
3. Extract the ZIP's `HutchASKA` folder into `ASKA\BepInEx\plugins\`, preserving its license, notices and build information. Both authored assemblies are required:

   ```text
   ASKA\BepInEx\plugins\HutchASKA\HutchASKA.Plugin.dll
   ASKA\BepInEx\plugins\HutchASKA\HutchASKA.Core.dll
   ```

4. Launch a local single-player game, open the trainer with F8, and check Diagnostics before enabling a feature.

Keep both DLLs from the same release. To uninstall, close ASKA and remove the HutchASKA plugin folder. Removing the plugin does not undo changes already saved by inventory, world, or villager actions.

## Build From Source

The solution separates `HutchASKA.Core` (no game dependencies) from `HutchASKA.Plugin` (local game integration). The following commands are available. Run them from the repository root.

Set `ASKA_GAME_DIR` to your own ASKA folder containing `ASKA.exe`. This is an illustrative path, not a repository-specific installation setting:

```powershell
$env:ASKA_GAME_DIR = "C:\Program Files (x86)\Steam\steamapps\common\ASKA"
.\scripts\Build-Local.ps1
```

Without an explicit path, the helper discovers exactly one ASKA installation through Steam libraries; ambiguous or unavailable discovery requires `-AskaGameDir`. It builds `src\HutchASKA.Plugin\HutchASKA.Plugin.csproj` in Release configuration and checks reachable managed IMGUI methods for known unstripping-failure stubs using the installed Mono.Cecil and interop DLLs. Missing references produce an actionable error. This static check cannot prove native rendering behavior. The installation helper builds first, refuses installation while ASKA is running and copies only HutchASKA-authored plugin and core DLLs:

```powershell
.\scripts\Install-Local.ps1
```

The pure core is testable without an ASKA installation:

```powershell
dotnet test tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj --configuration Release
```

Create a local development candidate with PowerShell 7:

```powershell
.\scripts\Package-Release.ps1
```

Packaging rebuilds without debug records containing local paths, copies only an explicit file allowlist, validates assembly identities and ZIP contents, and writes `artifacts/HutchASKA-v0.1.1.zip`. Both authored DLLs are required. It includes README.txt, LICENSE, THIRD_PARTY_NOTICES.md, BUILDINFO.txt and three runtime-license texts. BUILDINFO records the source commit and whether changes were uncommitted. Game, BepInEx, Harmony and generated interop binaries are rejected. A package is not evidence of gameplay acceptance.

Repository-relative research/checklist links in the packaged README refer to the matching source checkout identified in BUILDINFO; those source documents are not duplicated in the ZIP.

Public CI validates only the pure core using Node 24 actions and the .NET 8 SDK; .NET 6 is also installed to execute the net6 test assembly. Compilation against local ASKA references and in-game smoke tests remain necessary to validate the plugin.

## Local ASKA References

See [libs/README.md](libs/README.md) for the required local assembly names and provenance. References resolve from `ASKA_GAME_DIR`; machine-specific paths must remain in the local environment. Generate interop with the installed game and BepInEx rather than obtaining game DLLs from this repository.

Game, Unity interop, and BepInEx references use `Private=false` and are not copied into build output or packaged releases. Never copy game/generated interop DLLs into this public source tree, including ignored directories. Do not commit game DLLs, assets, saves, credentials, configuration or local path settings.

## Controls

Implemented defaults; their in-game interaction remains part of the manual smoke matrix:

| Key / control | Action |
| --- | --- |
| F8 | Show/hide the trainer |
| F1 | Toggle God Mode |
| F2 | Toggle infinite stamina |
| F5 | Toggle freeze world time |
| Reset All | Disable active cheats and restore native behavior where technically possible |

All seven tabs contain controls or explicit compatibility reasons. BepInEx configuration stores hotkeys, movement/game-speed selections, verbosity and optional enabled-state persistence. Duplicate gameplay hotkeys are ignored and the menu key takes priority. Set `RestoreEnabledStatesOnLaunch` only to explicitly opt in; restoration waits for positive single-player confirmation. Configuration is stored in `BepInEx/config/com.jfhutchi.hutchaska.cfg`, never in game saves.

Reload Configuration leaves gameplay features off; edited saved flags are considered only at the next launch. Rescan disables features before probing and does not reinstall patches automatically. Reset All clears both editors, resets controller selections and retries pending native cleanup, including failed UI cleanup, while preserving healthy menu input access. Runtime-faulted features remain faulted until restart after correction.

The menu preserves cursor state and uses an owned ASKA input context while open. Input suppression and native-menu interaction still require manual verification. Game speed respects native zero-scale pause states and restores the captured baseline; movement removes only its own native modifier. Reset All retries failed native cleanup where the target is still safely available. A scene-replaced target faults visibly rather than touching a stale native wrapper.

## Single-Player Safety

HutchASKA is intended only for local single-player play. Its required session guard permits gameplay changes only after positively confirming a local session. Confirmed multiplayer/co-op and unknown session states must block gameplay features; unknown state reports `Single-player state not confirmed`.

This is a required implementation contract, not a claim that the guard has passed runtime testing. Back up saves before testing. Reset All restores ongoing behavior where possible, but cannot undo item grants, completed crafting, recruitment, or other changes already made to the world or saved game.

## Steam Achievements

HutchASKA must not hook, patch, call, suppress, unlock, or otherwise modify Steam achievement APIs. Cheats can make normal gameplay achievements easier to obtain. The project cannot guarantee how current or future ASKA versions handle achievements when mods are installed.

## Updating After an ASKA Patch

1. Close ASKA and preserve a save backup before testing the updated game.
2. Verify BepInEx compatibility and regenerate interop when necessary using the applicable BepInEx regeneration procedure.
3. Point `ASKA_GAME_DIR` at that installation, rebuild HutchASKA against the current references, and inspect any changed signatures locally.
4. Check Diagnostics and repeat relevant single-player smoke tests, including disabling each feature and Reset All.

Missing or changed hooks must leave the affected feature incompatible or disabled with a reason. Never treat an old successful build as evidence of current runtime support, or copy stale/proprietary DLLs into Git to resolve an update.

## Diagnostics and Logs

Diagnostics reports HutchASKA, the actual Steam build from the matching manifest, ASKA application version, Unity and BepInEx; the single-player decision; and each feature's state, reason and pending cleanup. Application.version is not the Steam build ID; unavailable manifest detection is reported explicitly. States include Disabled, Enabled, Blocked, Incompatible and Faulted. Repeated errors disable the affected feature; shared session/population discovery also has bounded failure handling.

Full exceptions belong in the local `ASKA\BepInEx\LogOutput.log`; the UI should show a concise explanation. For a bug report, include versions, session type, reproduction steps, the affected feature, and the relevant log excerpt. Review logs for personal paths or other private information before sharing. Do not attach game DLLs or save files to public reports.

## Known Limitations

- Plugin startup was observed and all stages build locally, but gameplay has not passed the in-game smoke matrix. The generated ZIP is a development candidate only.
- Temperature, +/-1 hour, durability, freshness, retention, free craft/build/repair, aging and instant normal recruitment are Incompatible until narrow native behavior is verified. Age and warmth editing remain unavailable.
- Startup and detected versions do not establish gameplay compatibility. F8/input/cursor behavior, real single-player/co-op gates, pause/transition restoration, item initialization/save persistence and tribe membership/edit persistence require manual acceptance.
- Multiplayer/co-op, network manipulation, achievement modification, DRM/access-control bypass, and external memory trainers are outside scope.
- Exact game hooks require local assembly inspection; research notes alone do not verify every signature.
- Runtime object lifecycle, inventory/save persistence, input behavior, and restoration need in-game testing beyond pure-core tests.
- Fixed process offsets and pointer chains are not planned for v1.

## Contributing

Read the [design specification](docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md), [interop observations](docs/research/2026-09-14-aska-interop-observations.md), and [ordered implementation plans](docs/superpowers/plans/README.md) before making changes. Use a feature branch, follow stage gates, and keep core state independent of proprietary references.

Verify exact current game APIs locally instead of guessing member names. Prefer a narrow native API or isolated patch, preserve ASKA's inventory/construction/recruitment lifecycle, and keep features independently disableable. Contributions should include meaningful core tests where applicable, local build results, runtime evidence or an explicit verification gap, and documentation/changelog updates.

Before committing, inspect `git status --short` and run `git ls-files '*.dll' '*.exe' '*.assets' '*.bundle'`. The repository must contain no proprietary game binaries. Keep installation paths and local dependency copies out of commits.

## License

HutchASKA-authored source and documentation are licensed under the [MIT License](LICENSE), copyright 2026 jfhutchi. This grant does not license ASKA, game assets, or dependencies owned by others.

## Third-Party / Game Assets

ASKA and its assets belong to their respective rights holders and must be obtained separately. No `Assembly-CSharp.dll`, `SandSailorStudio.dll`, generated interop assemblies, or ASKA assets are redistributed here.

BepInEx, HarmonyX/HarmonyLib, Il2CppInterop, Unity and Photon/Fusion retain their own terms. Verified versions and license sources are recorded in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), with runtime license texts under `licenses/`. No WeMod code, game assets, runtime dependency DLLs or private configuration are included.

