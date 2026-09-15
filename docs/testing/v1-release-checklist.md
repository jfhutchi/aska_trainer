# HutchASKA 0.1.5 development candidate acceptance

Date: 2026-09-14. **NOT RELEASE-READY: MANUAL VERIFICATION REQUIRED.** No save was selected or modified by the implementation agent. This checklist separates automated/source evidence from gameplay acceptance.

User retest update: 0.1.2 screenshots show readable Player/Items/Crafting panels and 1,136 catalog entries. The user reports approximately 115 FPS; the follow-up Player screenshot shows God Mode, Infinite Stamina, Infinite Hunger and Infinite Thirst checked, with movement Disabled at a saved 1.5x setting. This is user-observed performance, not a controlled benchmark or verification of the native effects. See [retest evidence and remaining limits](readability-performance-repair.md).

## Environment actually observed

| Component | Observed |
| --- | --- |
| ASKA Steam build | 25186770, local installed manifest |
| Full game version | 1.43.0809261352._PC.Release, fresh Unity log |
| Application.version | 0.4, deliberately separate from Steam build |
| Unity | 6000.3.12f1 |
| BepInEx | 6.0.0-be.755+3fab71a1914132a1ce3a545caf3192da603f2258, IL2CPP |
| Game runtime | .NET 6.0.7 |
| HarmonyX | 2.10.2 |
| Il2CppInterop | 1.5.1-ci.829+6d9007c18cc8440830379c5e1d5714085e7ec577 |
| Local build/test tools | SDK 8.0.423, test runtime 6.0.36, PowerShell 7.6.5 |

The installed game still matches the original build target. A future update requires new evidence.

## Automated and static results

| Check | Result |
| --- | --- |
| Release core test suite | PASS: 175 tests, zero failures/skips |
| Local plugin build using actual installation references | PASS: zero warnings/errors, both authored assemblies version 0.1.5 |
| Managed IMGUI unstripping regression | PASS: 78 reachable methods contain no unstripping failure stubs. Native 0.1.3 Close retest pending |
| Red/green development | Core scaffold, registry, guard/breaker, settings, catalog, tribe contracts and rescan/reset tests were run failing before implementation and passing after |
| Runtime isolation | Pure tests cover unknown/multiplayer block, session loss, partial enable, bounded failure and explicit cleanup retry |
| Native hook scope review | Current local player/owned tribe gates; narrow movement, harvest, temperature, item loss/use, crafting and recruitment callbacks; no arbitrary spawn or achievement patches. Consumable removal patch requires the exact armed use/item/container and passes all other removals through |
| Git tracked DLL/EXE/assets/bundles | PASS: none |
| Local paths/secrets in authored source | No local installation/user path, credentials, saves or raw game logs added |
| Release archive generation | PASS: strict allowlist and authored assembly identity checks |
| Forbidden-package test | PASS: synthetic Assembly-CSharp.dll filename rejected before packaging; no actual game DLL copied |
| DLL debug record check | PASS: packaged builds contain no CodeView record with a local PDB/build path |
| MIT license | Standard text, Copyright (c) 2026 jfhutchi |
| Dependency notices | Actual assembly/package versions checked against linked upstream licenses; runtime texts included |
| Public CI | Configured for pure core; no remote CI run or gameplay result claimed |

Final packaged source commit is recorded in ZIP `BUILDINFO.txt`. The current candidate name is `artifacts/HutchASKA-v0.1.5.zip`; it is a local candidate, not a published release. Its exact nine files are the two authored DLLs, README.txt, LICENSE, THIRD_PARTY_NOTICES.md, BUILDINFO.txt, and three runtime-license texts under licenses/. Runtime dependencies, game binaries/assets, configuration, PDBs and saves are absent.

## Startup evidence

The 0.1.4 runtime failed movement acceptance, exposed Build Speed's ambiguous
property probe and crashed while extending a 20-tile leveling preview. Version
0.1.5 fixes the two guards/probes and withdraws expanded leveling. The user
confirmed Instant Summon in 0.1.3; axe logs show 4x application in 0.1.4. These
observations do not validate the other controls. Current correction and retest:
[0.1.5 evidence](0.1.5-corrective-retest.md).

Version 0.1.3 loaded, but the user reports no movement-speed effect and no
acceleration of tool harvesting. Those are failed gameplay checks. Version
0.1.4 corrects the missed movement command path and the ordinary-tool guard,
adds construction/repairs/build speed/terrain/fuel features, and requires a fresh
retest. See [0.1.4 checks](0.1.4-gameplay-retest.md). Historical startup evidence
below is not acceptance of the current candidate.

The user confirmed 0.1.1 menu rendering, but reported poor readability and approximately 10 FPS. Version 0.1.2's custom skin and lifecycle-invalidated discovery received readable screenshots and a user report of approximately 115 FPS. This is not a controlled benchmark. The 0.1.3 movement/Close repairs and expanded controls await a new game launch and gameplay test. See [0.1.3 retest](movement-harvest-close-retest.md).

The following evidence is historical for 0.1.0. The user's subsequent menu interaction exposed a repeated toolbar unstripping exception. Version 0.1.1 removed that call path; the later user screenshot confirms the controls render. See [GUI repair evidence](gui-rendering-repair.md).

PASS: final combined Player/World/Items/Tribe/Diagnostics DLLs extracted from the candidate ZIP loaded through a fresh BepInEx startup. The log records HutchASKA 0.1.0, Steam build 25186770, application version 0.4, Unity 6000.3.12f1, BepInEx 6.0.0-be.755, IL2CPP behaviour registration, all 11 expected Incompatible controls and successful chainloader completion. Unknown session was blocked and no HutchASKA exception was observed. No F8 interaction or gameplay acceptance is claimed. Earlier Stage 1 and intermediate startups also passed.

The initial loader issue was resolved locally with Doorstop ignore_disable_switch=true after preserving the original configuration outside Git. The installer does not alter Doorstop. Details: [loader record](local-loader-check.md).

## Required manual acceptance matrix

Use a backed-up/disposable single-player save. Record actual versions, enable/disable results and sanitized evidence for each row. For incompatible features, first establish a narrow hook before any activation test; their current expected behavior is a disabled control with its reason.

| Feature / scenario | Current result and required check |
| --- | --- |
| First installation and persistence | MANUAL VERIFICATION REQUIRED: all cheats off; opt-in restore waits for confirmed single-player; disabled defaults survive restart |
| F8, Close, dragging, tabs, resolution | MANUAL VERIFICATION REQUIRED: one window, usable controls/scrolling, no duplicate shell after reload |
| Input/cursor | MANUAL VERIFICATION REQUIRED: no leaked gameplay input while open; cursor restored on close, unload and native-menu transitions |
| F1/F2/F5/configured/duplicate keys | MANUAL VERIFICATION REQUIRED: correct target toggles; recovery after blocked session; menu priority |
| Session gate | MANUAL VERIFICATION REQUIRED: Unknown/co-op/connecting/disconnecting never permit changes; local session is positively recognized |
| Player God Mode | MANUAL VERIFICATION REQUIRED: local incoming damage blocked; others unaffected; native damage resumes after disable |
| Stamina | MANUAL VERIFICATION REQUIRED: relevant native actions do not drain local stamina; others unaffected; disable restores drain |
| Player hunger/thirst | MANUAL VERIFICATION REQUIRED: each independent, finite native maxima, normal depletion after disable |
| Movement | MANUAL VERIFICATION REQUIRED: 1x-5x, native modifiers preserved, disable/reset/preset changes, death/reload/scene transition cleanup |
| Harvesting | MANUAL VERIFICATION REQUIRED: 1x/2x/3x/4x hand-gather and tool-harvest cycles, cancellation, amount per completion, combat/movement unaffected, 1x/reset/disable and FPS |
| Build Speed | MANUAL VERIFICATION REQUIRED: 1x/2x/3x/4x work per stroke, supplied and free-material cases, native costs and completion, cancellation/reset |
| Terrain Leveling | WITHDRAWN: 20-tile preview crashed while extending second side. 10x10 not tried. No enlargement hooks in 0.1.5; normal leveling only |
| Infinite Fuel | MANUAL VERIFICATION REQUIRED: campfire and standing torch fuel drain, initial fuel/ignition, normal refueling/extinguishing/weather, disable and persistence |
| World freeze | MANUAL VERIFICATION REQUIRED: native clock pauses/resumes; original stopped state preserved; unload safely handled |
| Global game speed | MANUAL VERIFICATION REQUIRED: all presets, non-unit baseline, native pause/resume and Reset All without forced unpause |
| Item catalog | MANUAL VERIFICATION REQUIRED: runtime definitions, display/internal search, pages, selection clearing and explicit give target |
| Give Item/Give Stack | MANUAL VERIFICATION REQUIRED: initialization, native stack rules, full inventory/errors, partial insertion warning, save/exit/reload persistence |
| Tribe membership | MANUAL VERIFICATION REQUIRED: owned living villagers included; guests/enemies/other settlements/dead excluded; ambiguous IDs fail closed |
| Tribe invincibility | MANUAL VERIFICATION REQUIRED: owned damage blocked; player/animals/enemies unaffected; disable restores native damage |
| Tribe food/water/energy/rest/happiness | MANUAL VERIFICATION REQUIRED: separate toggles, native caps, new recruits included, no cached wrappers across reload |
| Heal tribe / Restore All Needs | MANUAL VERIFICATION REQUIRED: one-shot only, correct members/fields, temperature/lifetime unchanged, failures visible |
| Villager editor | MANUAL VERIFICATION REQUIRED: stable selection, only changed fields applied, native ranges/caps, disappearance clears selection, persistence after reload |
| Advanced / cleanup | MANUAL VERIFICATION REQUIRED: reload leaves cheats off, rescan causes no duplicate hooks, Reset All restores values and retries pending input cleanup, editors clear |
| Runtime error isolation | MANUAL VERIFICATION REQUIRED: broken feature faults visibly without repeated native exceptions or disabling unrelated features |
| Temperature (player/tribe) | MANUAL VERIFICATION REQUIRED: prevents further cooling/frost, preserves warming/thawing, current owned scope, disable. Existing freezing is not cured; warmth editing remains read-only |
| -1/+1 hour | MANUAL VERIFICATION REQUIRED: same-day steps, native forward-midnight carry, backward-midnight rejection, freeze preserved, lighting/weather/events |
| Durability / freshness | MANUAL VERIFICATION REQUIRED: partially worn tools/carried perishables preserve condition, independent toggles, normal decay after disable, repairs, equipment/drop/storage and save/reload |
| Retain Consumables On Use | MANUAL VERIFICATION REQUIRED: native effects and animation, one-item/larger stacks, unrelated spending, disable and save/reload |
| Ignore Crafting Materials | MANUAL VERIFICATION REQUIRED: zero materials, existing station/player materials unchanged, locked recipes and blueprint costs preserved, cancellation, native output, disable and persistence |
| Free Building / Free Repairs | MANUAL VERIFICATION REQUIRED: zero/partial supplies, native work/layers/completion, disable, cancellation and save/reload. Granted repair work remains available after disabling |
| Age editing / Freeze Aging | Unavailable/Incompatible: inspected lifetime modifier is initialized for golems, not a confirmed ordinary-villager aging hook |
| Instant normal recruitment | MANUAL VERIFICATION REQUIRED: two normal recruits, costs/names/traits/AI/population/save, owned settlement, disable and no runaway repetition |
| Steam achievements | Source audit: no API manipulation; actual ASKA mod/achievement behavior is not guaranteed |

Stage-specific procedures remain in [Stage 1](stage-1-smoke-test.md), [Stage 2](stage-2-smoke-test.md), [Stage 3](stage-3-smoke-test.md) and [Stage 4](stage-4-smoke-test.md). Current incompatible features fail closed as authorized; none are presented as working cheats. Manual checks and uncertain native hook semantics are the remaining external/runtime work.
