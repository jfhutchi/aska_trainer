# HutchASKA

HutchASKA is a free, open-source, single-player in-game trainer for the Steam game ASKA, implemented as a BepInEx 6 IL2CPP plugin.

> HutchASKA is an unofficial community project. It is not affiliated with or endorsed by Sand Sailor Studio, Thunderful, Steam, Valve, or WeMod.

## Status

Early development toward version `0.1.0`. The pure core, local build/install scripts, fail-closed session contract, and F8 diagnostics shell are implemented. The core tests and local plugin build pass; gameplay features are still planned. In-game verification remains required. All cheats must start off on first installation, and automatic restoration of enabled states must default to off.

Source projects and local build/install helpers are available. No validated release archive has been published. An in-game smoke test is required before a release can claim runtime compatibility.

## Compatibility

These versions are the initial target recorded in the local interop research and detected in the local Steam manifest and BepInEx startup log on 2026-09-14. They are **not a tested HutchASKA release matrix**; plugin loading and gameplay behavior still require verification.

| Component | Initial target | Verification status |
| --- | --- | --- |
| ASKA Steam build | `25186770` | Detected in local Steam manifest; HutchASKA runtime test pending |
| Unity | `6000.3.12f1` | Detected in BepInEx startup log; HutchASKA runtime test pending |
| BepInEx | `6.0.0-be.755`, IL2CPP | Detected in startup log; HutchASKA runtime test pending |
| Runtime / target framework | `.NET 6.0.7` / `net6.0` | Runtime detected in startup log; initial compile target |
| Platform / session | Windows, local single-player | Multiplayer/co-op unsupported |

Future release notes must state the exact ASKA, Unity, and BepInEx versions tested and any incompatible features. A game update may invalidate individual hooks even if the plugin still loads.

## Features

Every entry below is **planned, unavailable at this milestone, and off by default when implemented**. One-shot actions run only on explicit user input.

| Area | Planned controls |
| --- | --- |
| Player | God Mode, infinite stamina, independent hunger/thirst/temperature controls, movement speed from 1.0x to 5.0x |
| Items | Prevent future durability/freshness loss, retain stack quantity on use, searchable runtime Item Browser with quantity selection |
| Crafting & Building | Free crafting, construction, and repairs through native completion flows |
| World | Freeze world time, -1/+1 hour adjustment, separate game-speed presets of 0.5x/1.0x/2.0x/5.0x |
| Tribe | Invincibility, hunger/thirst/temperature/energy/rest/happiness/aging controls, heal tribe and restore needs actions |
| Villagers | Individual health/needs/age editing and accelerated native recruitment, subject to verified game APIs |
| Advanced & Diagnostics | Reset All, optional state persistence, configurable hotkeys, compatibility checks, feature states and error reasons |

God Mode must block damage without inflating maximum health. Durability and freshness toggles preserve existing values. Add Items On Use retains quantity instead of duplicating arbitrary items. Recruitment must complete ASKA's normal lifecycle; arbitrary villager spawning is out of scope. Blueprint requirement bypass is optional and depends on a verified narrow hook.

## Requirements

- A local Windows installation of ASKA acquired separately through Steam.
- A compatible BepInEx 6 IL2CPP installation acquired separately. No ASKA binaries, BepInEx runtime bundle, or generated interop binaries are included here.
- A first ASKA launch after installing BepInEx, allowing generation of `BepInEx\interop`; close the game before building or installing HutchASKA.
- For source builds: PowerShell, Git, and the .NET 8 SDK (C# 12 source, targeting `net6.0`). Running the core tests also requires a compatible .NET 6 runtime.

## Install a Release

No validated release is available at this milestone. When a release is published:

1. Read its compatibility matrix and known limitations. Back up the save you intend to use.
2. Install the specified BepInEx IL2CPP build separately, launch ASKA once to generate interop, then exit ASKA.
3. Extract the HutchASKA release into `ASKA\BepInEx\plugins\HutchASKA\`, preserving its companion files. Both authored assemblies are required:

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

The helper builds `src\HutchASKA.Plugin\HutchASKA.Plugin.csproj` in Release configuration. Missing or mismatched local reference files must produce an actionable build error. The local installation helper builds first and copies only HutchASKA-authored plugin and core DLLs:

```powershell
.\scripts\Install-Local.ps1
```

The pure core is testable without an ASKA installation:

```powershell
dotnet test tests\HutchASKA.Core.Tests\HutchASKA.Core.Tests.csproj --configuration Release
```

Public CI validates only the pure core using Node 24 actions and the .NET 8 SDK; .NET 6 is also installed to execute the net6 test assembly. Compilation against local ASKA references and in-game smoke tests remain necessary to validate the plugin.

## Local ASKA References

See [libs/README.md](libs/README.md) for the required local assembly names and provenance. References resolve from `ASKA_GAME_DIR`; machine-specific paths must remain in the local environment. Generate interop with the installed game and BepInEx rather than obtaining game DLLs from this repository.

Game, Unity interop, and BepInEx references must not be copied into build output or packaged releases. `libs/local/` is ignored for local scratch dependencies; it is not a distribution folder. Do not commit game DLLs, generated interop, assets, saves, or local path settings.

## Controls

These are planned defaults and are not available yet:

| Key / control | Action |
| --- | --- |
| F8 | Show/hide the trainer |
| F1 | Toggle God Mode |
| F2 | Toggle infinite stamina |
| F5 | Toggle freeze world time |
| Reset All | Disable active cheats and restore native behavior where technically possible |

Other features are menu-first unless a hotkey is configured. Planned tabs are Player, Items, Crafting & Building, World, Tribe, Advanced, and Diagnostics. BepInEx configuration will store hotkeys, multipliers, verbosity, and optional toggle persistence; trainer configuration does not belong in game saves.

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

The planned Diagnostics tab reports HutchASKA, ASKA (when detectable), Unity, and BepInEx versions; the single-player decision; and each feature's state and failure reason. Expected feature states include Disabled, Enabled, Blocked, Incompatible, and Faulted. Repeated feature errors must disable that feature without taking down unrelated features.

Full exceptions belong in the local `ASKA\BepInEx\LogOutput.log`; the UI should show a concise explanation. For a bug report, include versions, session type, reproduction steps, the affected feature, and the relevant log excerpt. Review logs for personal paths or other private information before sharing. Do not attach game DLLs or save files to public reports.

## Known Limitations

- The diagnostics shell builds locally but has not passed the in-game smoke matrix; no validated release ZIP exists.
- Detected game/runtime versions have not established HutchASKA compatibility with the current installed game.
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

BepInEx, HarmonyX/HarmonyLib, Il2CppInterop, Unity, and other third-party components retain their own licenses and terms. Repository licensing does not replace them. Dependency notices and redistribution terms must be reviewed and recorded in `THIRD_PARTY_NOTICES.md` before the first release; that release document is not yet present. HutchASKA must not include WeMod code or attempt to bypass its restrictions.

