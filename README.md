# HutchASKA

HutchASKA is a free, open-source, single-player in-game trainer for the Steam game ASKA, implemented as a BepInEx 6 IL2CPP plugin.

> HutchASKA is an unofficial community project. It is not affiliated with or endorsed by Sand Sailor Studio, Thunderful, Steam, Valve, or WeMod.

## Status

Version 0.1.15 moves Instant Normal Recruitment and recruit trait reroll into a
separate Tribe Summon tab. The Tribe tab retains villager needs, skills, speeds,
and the villager editor. The nine menu tabs now use three rows of three. This
menu change still needs an in-game check.

Version 0.1.14 starts trainer options off each time a world loads, including
when returning through the main menu without quitting ASKA. The former
restore-on-launch option is retired and saved enabled flags are cleared.
Multiplier presets remain saved but inactive until the corresponding option is
enabled. Easy Catch and Rare Fish Anywhere also reset to off. This change
awaits an in-game world-transition retest.

Version 0.1.13 keeps Free Building from waiving farm-plot supplies after a
confirmed farm activation crash. Farm plots require normal materials even when
Free Building is on; other eligible structures still receive the supply waiver.
The user confirmed farm-plot construction works with Free Building enabled in
the 0.1.13 in-game retest. Version 0.1.12 kept the readable
opaque menu and active gameplay toggles working when ASKA returns to its main
menu and starts or loads another single-player world.
The main-menu/new-world transition, Free Building rearm and menu controls were
user-confirmed in the 0.1.12 native retest. Weather remains a per-world choice.
The user also confirmed the basic effects of Tribe Skill Gain, Tribe Harvest
Speed, Infinite Fuel, Free Repairs, Ignore Crafting Materials, Retain Items on
Use, world-time/game-speed controls, player/tribe protection and needs, and Give
Item/Give Stack in the installed 0.1.12 build.
Version 0.1.11 adds Rare Fish Anywhere, which removes the local rare-fish-school
requirement while retaining native bait, season and biome checks. Version 0.1.10
corrects Tribe Movement Speed eligibility while retaining native
speed for swimming, vehicles, ladders and special link traversal. Version 0.1.9
added World-tab weather choices: Normal Forecast, Clear Skies,
Rain, Fog, Overcast and Blizzard, using ordinary weather assets loaded by the
current world. The selection is session-only, pauses for special-event weather,
and does not reroll the forecast or move the clock. Normal/reset resumes native
weather; resulting wetness, snow and normal gameplay effects can persist and
clear naturally. Missing weather assets are reported as unavailable.

Player Skill Gain and Tribe Skill Gain have separate
normal/2x/3x/4x/5x presets. The Tribe tab also offers independent Build Speed and
Harvest Speed presets for current owned tribe members, plus ground Movement
Speed presets through 5x. Player Harvesting Speed
is in Fishing & Foraging, and the menu uses the game-window height with small
top/bottom margins. These changes require in-game acceptance after installation;
see the [0.1.9 retest](docs/testing/0.1.9-weather-and-tribe-retest.md).

Skill gain multiplies positive experience earned through ASKA's normal award
routine, including work, fishing and combat. It leaves native level-up and cap
checks intact. Normal/reset stops future bonuses; already earned experience
and completed work remain normal game progress and can be saved. Tribe build
and tool-harvest presets increase work per action, not walking or animation
speed. They do not bypass material requirements or multiply item quantities.
Tribe Movement Speed affects ordinary ground navigation; swimming, vehicles,
ladders and special link traversal remain native. Acceleration and pathfinding
remain under game control. Existing tribe protections and needs are separate
toggles, while Free Building/Free Repairs already operate on shared eligible
structures. Fishing assists and consumable-retention transactions remain scoped
to the player; the parity audit records further villager-specific work.

**Version `0.1.15` is a development candidate requiring gameplay acceptance.**
Fishing & Foraging offers bite-speed presets (1x/2x/4x), eligible rare-fish-weight
presets (normal/20x/30x/40x/50x) and Easy Catch. Rare weights are relative weights,
not guaranteed catch percentages; bait and native eligibility still apply.
Old 2x/4x rare settings reset to normal when upgrading. Mushroom regrowth remains
unavailable. Tribe offers a traits-only recruit
preview and Apply action. These features preserve ordinary gameplay actions;
the trainer does not set achievements or their progress counters. See the
[0.1.7 retest](docs/testing/0.1.7-gameplay-assists-retest.md).

Movement retains the user-confirmed 0.1.5 implementation. The 0.1.6 replacement
for stalled building progress remains in place and still needs a fresh gameplay
retest; see [building evidence](docs/testing/0.1.6-building-retest.md).
Infinite Durability and No Spoilage remain unavailable after item-processing
errors. Freeze Aging and expanded leveling also remain unavailable. Normal game
wear, spoilage and leveling apply. Trainer options start off on every world load.

The user reported approximately 115 FPS and supplied readable screenshots for 0.1.2 after the earlier readability/performance repair. That observation does not validate the new controls or their FPS impact. See the [0.1.3 retest](docs/testing/movement-harvest-close-retest.md), [release checklist](docs/testing/v1-release-checklist.md), [earlier repair evidence](docs/testing/readability-performance-repair.md) and [implementation ledger](docs/testing/implementation-status.md).

Source projects and local build/install helpers are available. No validated release archive has been published. An in-game smoke test is required before a release can claim runtime compatibility.

## Compatibility

These versions were checked on 2026-09-21 using the local Steam manifest, installed assembly metadata and latest game logs. ASKA updated from the original 25186770 target to 25440748; the replacement building calculation and configuration path were re-inspected against the updated native binary. They are **not a tested gameplay release matrix**; startup does not verify behavior in a save.

| Component | Detected version | Evidence |
| --- | --- | --- |
| ASKA Steam build | `25440748` | Detected in installed Steam manifest and HutchASKA 0.1.11 startup log |
| Full game version | `1.44.1509261752._PC.Release` | Detected in fresh Unity log |
| Application.version | `0.4` | Separate application value, not the Steam build ID |
| Unity | `6000.3.12f1` | Detected at plugin startup |
| BepInEx | `6.0.0-be.755`, IL2CPP | Commit `3fab71a1914132a1ce3a545caf3192da603f2258`, detected at startup |
| Runtime / target framework | `.NET 6.0.7` / `net6.0` | Runtime detected in startup log; initial compile target |
| Platform / session | Windows, local single-player | Multiplayer/co-op unsupported |

Installed HarmonyX is `2.10.2`; Il2CppInterop is `1.5.1-ci.829`, commit `6d9007c18cc8440830379c5e1d5714085e7ec577`. Local development uses .NET SDK `8.0.423`, test runtime `6.0.36` and PowerShell `7.6.5`.

Future release notes must state the exact ASKA, Unity, and BepInEx versions tested and any incompatible features. A game update may invalidate individual hooks even if the plugin still loads.

The user reported that movement and tool harvesting were ineffective in 0.1.3. See the [0.1.4 follow-up retest](docs/testing/0.1.4-gameplay-retest.md) for the corrected native paths, bounded diagnostics and new-control checks, and the [unfinished-options roadmap](docs/research/disabled-options-roadmap.md) for remaining limitations.

## Features

Implemented controls compile against current local interop signatures and are **not yet verified in a save**. Incompatible controls are visible, disabled and explain their reason. All gameplay toggles default off.

| Area | Controls and implementation status |
| --- | --- |
| Player | Implemented: God Mode, infinite stamina, independent hunger/thirst, protection against further cooling/frost, on-foot movement 1.0x-5.0x. Native retests pending |
| Items | Implemented: runtime catalog/search, native Give Item/Give Stack and retaining local consumables after their normal use effects. Infinite Durability and No Spoilage are temporarily disabled after item-processing failures |
| Crafting & Building | Implemented: Ignore Crafting Materials, Free Building, Free Repairs and player construction work presets 1x/2x/3x/4x. Expanded leveling is disabled after the 0.1.4 preview crash |
| Fishing & Foraging | Player harvesting presets 1x/2x/3x/4x; new: bite speed 1x/2x/4x, Easy Catch, eligible rare-fish weighting 1x/2x/4x (mushroom regrowth is unavailable). Normal catch/gather actions remain required; live acceptance pending |
| World | Implemented: Freeze Time, -1/+1 hour, separate game-speed presets 0.5x/1x/2x/5x and Infinite Fuel for campfires/standing torches. Backward time adjustment cannot cross midnight |
| Tribe | Implemented: current owned-villager damage suppression and cooling/frost protection; independent food/water/energy/rest/happiness maintenance in one shared pass; Heal Entire Tribe; Restore All Needs. Freeze Aging remains Incompatible |
| Villagers | Implemented: name/ID search, fresh-ID resolution, changed-field-only health/food/water/energy/rest/happiness edits, Heal/Max Needs/Apply, and instant normal recruitment through the pending owned outlet's native completion. New: preview and apply starting traits to ordinary unsummoned recruit choices, retaining identity/appearance. Warmth is read-only; age is unavailable |
| Advanced & Diagnostics | Implemented: Reset All, config reload, compatibility rescan, logging verbosity, optional state persistence, configurable hotkeys, actual Steam build detection, feature states and error reasons |

God Mode must block damage without inflating maximum health. Add Items On Use retains quantity instead of duplicating arbitrary items. Recruitment must complete ASKA's normal lifecycle; arbitrary villager spawning is out of scope. Blueprint requirement bypass is optional and depends on a verified narrow hook.

Infinite Durability and No Spoilage cannot activate in 0.1.7, including through restored configuration. Their failing hooks have been removed; normal game wear and spoilage apply. Temperature protection prevents further cooling/frost accumulation; existing freezing penalties can remain until normal warming/thawing. Retain Consumables On Use preserves a carried consumable's quantity only after its native player-use effects, including the last item in a stack. Unrelated item spending remains native. Give uses ASKA's native definition-based insertion, checks ownership/capacity, limits quantities to 1-999 and measures the amount actually added. Initialization and save persistence still need acceptance.

Ignore Crafting Materials waives the temporary recipe-material manifest in the normal local-player crafting path. It leaves consumable blueprint-item costs, unlocks, station eligibility, crafting duration and product creation native; unrelated villager/cooking/forging paths are not automatically covered. Time adjustment changes the clock/weather through the native setter, preserving Freeze Time; it does not simulate an hour of work or survival. Moving forward across midnight uses native day advancement, while backward midnight crossing is refused with a visible reason.

All enabled tribe needs share one update every half second. The batch resolves current registered, living, locally owned members once, rechecks live membership before writes, and retains no raw villagers across calls. Guests and ambiguous IDs are excluded. Max Needs leaves warmth and lifetime untouched. Remaining lifetime is never presented as chronological age: the inspected expiry modifier is golem-specific. Recruitment overrides one deadline read inside the owned pending outlet's native completion callback; it does not write the saved timer or call spawning directly. Native costs, villager creation and rearm remain in ASKA's normal path. The item and villager lists are sorted only when their snapshot or search changes.

Free Building waives material checks for eligible current construction parts while keeping native work, layers and completion. Farm plots retain normal material requirements because bypassing their supply checks can activate the farm before its crop grid is initialized. Disabling rechecks actual supplies; completed work is retained. Free Repairs allows the normal repair-work phase without additional supplies. Any already deposited materials remain committed, and disabling does not revoke a repair phase already granted. Build Speed increases work per player hammer stroke, independently of material requirements.

Expanded-leveling presets remain unavailable in 0.1.7: the implementation contains no terrain hooks and cannot activate from saved configuration or rescan. The normal leveling tool is unchanged. The user's 20-tile preview crashed while extending the second side; 10x10 was not attempted. See the [0.1.5 corrective retest](docs/testing/0.1.5-corrective-retest.md).

Infinite Fuel prevents fuel drain on owned campfires and standing torches; add starting fuel and light them normally. It does not refill or ignite an empty fire.

## Fishing, mushrooms and recruit traits

Open **Fishing & Foraging** for the new presets. Faster bites shorten the native
wait; Easy Catch extends the reaction window fourfold and removes the random
failed-catch roll after a valid bite. Casting and reeling remain necessary.
Rare Fish Boost changes eligible special-fish weights relative to common fish;
2x/4x is not a promised catch percentage. Normal bait, biome and availability
rules, item processing and fishing progress events remain in the game path.

Mushroom Regrowth is unavailable in 0.1.8. Its speed buttons and timer hooks
are removed, activation is rejected even through restored settings, and its
multiplier is forced to normal on startup/reload. ASKA saves the accelerated
deadline but the old implementation retained its restoration value only in
memory. This update prevents new timer changes; it does not alter existing
saves or undo mushrooms that already regrew.

In **Tribe**, close the game's recruit-selection screen, then use **Refresh
recruits**, select an ordinary candidate, **Reroll preview**, and **Apply these
traits**. Reopen the normal recruitment screen to review and summon. This uses
native compatible perk generation and preserves name, definition, appearance and
portrait. A changed choice or active summon invalidates a preview. Ordinary
multi-choice recruitment is supported; special/single-choice and lost-villager
summons are excluded. Unchosen choices are regenerated by normal game loading;
confirm your selected recruit normally before relying on selected-data persistence.
Established villagers are unaffected. Rerolls only happen on explicit clicks.

Native evidence: [fishing](docs/research/2026-09-21-fishing-native-flow.md),
[mushroom regrowth](docs/research/2026-09-21-mushroom-regrowth.md), and
[recruit traits](docs/research/2026-09-21-recruit-reroll.md). Native analysis and
passing builds are not confirmation of gameplay or platform achievement unlocks.

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

Packaging rebuilds without debug records containing local paths, copies only an explicit file allowlist, validates assembly identities and ZIP contents, and writes `artifacts/HutchASKA-v0.1.6.zip`. Both authored DLLs are required. It includes README.txt, LICENSE, THIRD_PARTY_NOTICES.md, BUILDINFO.txt and three runtime-license texts. BUILDINFO records the source commit and whether changes were uncommitted. Game, BepInEx, Harmony and generated interop binaries are rejected. A package is not evidence of gameplay acceptance.

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

All seven tabs contain controls or explicit compatibility reasons. BepInEx configuration stores hotkeys, movement/harvesting/build-speed/terrain-size/game-speed selections, verbosity and optional enabled-state persistence. Duplicate gameplay hotkeys are ignored and the menu key takes priority. Set `RestoreEnabledStatesOnLaunch` only to explicitly opt in; restoration waits for positive single-player confirmation. Configuration is stored in `BepInEx/config/com.jfhutchi.hutchaska.cfg`, never in game saves.

Reload Configuration leaves gameplay features off; edited saved flags are considered only at the next launch. Rescan disables features before probing and does not reinstall patches automatically. Reset All clears both editors, resets controller selections and retries pending native cleanup, including failed UI cleanup, while preserving healthy menu input access. Runtime-faulted features remain faulted until restart after correction.

The menu preserves cursor state and uses an owned ASKA input context while open. Input suppression and native-menu interaction still require manual verification. Game speed respects native zero-scale pause states and restores the captured baseline; movement scales native on-foot command speed and complementary horizontal root-motion samples, restoring temporary settings after each call; harvesting restores only the animator speed it owns. Reset All retries failed native cleanup where the target is still safely available. A scene-replaced target faults visibly rather than touching a stale native wrapper.

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
- Expanded leveling is unavailable after a native preview crash; all enlargement hooks are removed. Freeze Aging remains Incompatible because ordinary-villager aging has not been established. Age and warmth editing remain unavailable. New native controls require live acceptance on the recorded ASKA build; see the supported scopes above.
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

