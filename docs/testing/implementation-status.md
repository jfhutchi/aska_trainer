# Ordered implementation ledger

## 0.1.12 user acceptance

The user confirmed the main-menu/new-world UI and feature-rearm repair, followed
by the basic effects of Tribe Skill Gain, Tribe Harvest Speed, Infinite Fuel,
Free Repairs, Ignore Crafting Materials, Retain Items on Use, Freeze Time,
minus/plus one hour, Game Speed, player/tribe protection and needs, and Give
Item/Give Stack. See [the transition retest](0.1.12-scene-transition-retest.md)
and [additional feature acceptance](0.1.12-feature-acceptance.md). Historical
pending-acceptance statements below describe the state when those versions were
built and do not override these later runtime reports.

## 0.1.7 gameplay assists

The approved fishing, mushroom-regrowth and recruit-trait work is implemented in
isolated feature modules. Fishing retains native catch/loot/deed processing;
mushrooms use three verified resources and bounded weather scheduling; recruit
traits are previewed and explicitly applied only to a still-current ordinary
choice. Root integrated the menu, defaults, reset/reload and registration.
The combined suite passes 220 cases. Independent subsystem and integration
reviews are recorded in the research documents and implementation plan.
Live acceptance remains required; see [0.1.7 checks](0.1.7-gameplay-assists-retest.md).

## 2026-09-21 status correction

Version 0.1.6 withdraws Infinite Durability and No Spoilage after the latest
0.1.5 log recorded 688,666 item-decay and 246 equipment-wear trampoline errors.
Both controls now reject activation and their hook implementations are removed.
Normal game wear/spoilage applies. Earlier implementation descriptions below
are historical, not current supported behavior. Replacement item protection is
outstanding. See [building and item-error evidence](0.1.6-building-retest.md).

Implementation used the dedicated `codex/hutchaska-implementation` worktree after the user's fresh pull. Main was not edited or merged. Each planned task was handled in order; corrective review commits are retained without rewriting history. The superseded non-final Stage 4 plan was not executed.

“Done” below means implementation, documented conditional fallback and available verification are complete. It does not mean a manual gameplay check passed. All remaining acceptance work is explicitly recorded in the [release checklist](v1-release-checklist.md).

## Stage 1

| Task | Disposition | Commit |
| --- | --- | --- |
| 1 README/license/hygiene | Done | f9bba18 |
| 2 Solution/core/tests | Done, red/green scaffold | 99fa0ad |
| 3 Feature registry | Done, red/green tests | b5098c6 |
| 4 Circuit breaker | Done, red/green tests | f19e4df |
| 5 Settings/hotkeys | Done, red/green tests | 4402ed0 |
| 6 Local references/build/install | Done, missing-reference gate and real build verified | 386996e |
| 7 Bootstrap/gate/diagnostics | Done, loader observed; F8 acceptance manual | 0b5c666 |
| 8 Core CI | Done, configured; local tests pass, remote CI not run | 789f21e |

## Stage 2

| Task | Disposition | Commit |
| --- | --- | --- |
| 1 Fresh game contexts | Done | 9450485 |
| 2 Player God Mode | Done, gameplay acceptance manual | 10670ef |
| 3 Stamina | Done, gameplay acceptance manual | 7e18ff3 |
| 4 Survival | Done; safe temperature hook unavailable and disabled | ea6b349 |
| 5 Movement | Done, reversible owned modifier; native acceptance manual | f4de755 |
| 6 Time/speed | Done; hour adjustment disabled for unknown units | 8cc688e |
| 7 UI/hotkeys | Done, input acceptance manual | f0e4cf4 |
| 8 Verification/docs | Done with explicit manual gaps | 6fd1248 |

Input lifecycle review fixes landed with the native Give Item task in 4d525a3 after a shared staging-index collision. No history was amended or discarded. Later commits were serialized.

## Stage 3

| Task | Disposition | Commit |
| --- | --- | --- |
| 1 Durability | Done via authorized Incompatible fallback; loss-only semantics unverified | 760bd7a |
| 2 Freshness | Done via Incompatible fallback; expiration semantics unverified | be2163b |
| 3 Retain on use | Done via Incompatible fallback; use-only decrement not proven | 9ebe725 |
| 4 Runtime catalog | Done, five red/green search tests | 33c4dfc |
| 5 Native give | Done implementation; initialization/save acceptance manual | 4d525a3 |
| 6 Free crafting | Done via Incompatible fallback; material-only transaction not proven | cb426ca |
| 7 Free building/repair | Done via independent Incompatible fallbacks | 65ded18 |
| 8 UI | Done; hidden-selection review correction in 16a92c5 | e72bdf0 |
| 9 Verification/docs | Done with explicit manual gaps | 71d5c6d |

## Stage 4 (final plan)

| Task | Disposition | Commit / evidence |
| --- | --- | --- |
| 1 Tribe contracts/adapter | Done, red/green tests and local API map | 0da3ba4 |
| 2 Villager invincibility | Done implementation; native scope/disable acceptance manual | 418d113 |
| 3 Needs/restoration | Done; temperature/aging disabled | 9b12d0d |
| 4 Individual editor/UI | Done; age unavailable, warmth read-only | 7fadb45 |
| 5 Normal recruitment | Done via Incompatible fallback; no arbitrary spawning | d2ab865 |
| 6 Diagnostics/reset | Done, reviewed rescan/reset tests and build | 7bc8f85 |
| 7 Packaging | Done; strict allowlist and forbidden-file rejection; Core version alignment in 07c1402 | dcf8237 |
| 8 Final docs/notices/verification | Done for available automated/static/startup work; manual release gate remains blocked | Release checklist and final documentation commit |

## Remaining external acceptance

Version 0.1.6 records user-confirmed movement in 0.1.5 and repairs the next
reported failure: native-to-managed work-call exceptions kept building at zero,
including after 1x was selected. Build Speed now uses private per-event settings
through the configuration getter; the native work submission stays unpatched.
189 core tests pass. A new local compiled-hook gate rejects the installed broken
implementation and passes the replacement. Expanded leveling remains withdrawn.
See [0.1.6 evidence](0.1.6-building-retest.md); live building retest is pending.

Version 0.1.5 responds to actual 0.1.4 failures: normal movement was blocked by
an inverted player-input permission check; Build Speed had a shadowed-property
lookup error; the 20-tile leveling preview crashed while extending its second
side. The checks are corrected and all terrain enlargement hooks are withdrawn.
175 core tests and the local build pass; compiled terrain containment was
inspected. Movement/build-speed gameplay retesting is still required. The user
confirmed Instant Summon in 0.1.3. See [0.1.5 evidence](0.1.5-corrective-retest.md).

Version 0.1.4 addresses the user's failed 0.1.3 movement and tool-harvest tests
with native velocity-path scaling and positive harvest-action guards. It adds
Free Building, Free Repairs, build-work presets, terrain plans up to 20x20 grid
tiles and campfire/standing-torch fuel protection. 164 pure core tests pass;
native acceptance remains pending. Freeze Aging remains unavailable. See
[0.1.4 retest](0.1.4-gameplay-retest.md). Historical rows and earlier-version
paragraphs record the state at those versions rather than current support.

Version 0.1.2 responded to the user's low-contrast screenshot and approximately 10 FPS report with an owned opaque GUI skin and lifecycle-invalidated manager discovery. Its 69 core tests and local build passed. The user subsequently supplied readable screenshots and reported approximately 115 FPS. This did not establish individual cheat effectiveness. See [readability/performance evidence](readability-performance-repair.md).

Version 0.1.3 expands the user's movement/Close repair request: physical root-motion control, fixed Close footer, harvesting 1x/2x/3x/4x, player/tribe cooling-frost protection, carried durability/spoilage, scoped consumable retention, material-only local crafting, normal pending recruitment and bounded native hour adjustment. Tribe needs share one half-second pass; list sorting and feature snapshots avoid repeated work. 120 core tests and the actual-reference build pass; native gameplay acceptance remains pending. Free Building, Free Repairs and Freeze Aging retain explicit implementation blockers. Historical stage rows above describe their original delivery, not current support. See [expanded retest](movement-harvest-close-retest.md) and [remaining roadmap](../research/disabled-options-roadmap.md).

Post-implementation repair: the user reported a native menu failure in 0.1.0. Version 0.1.1 replaces two stripped toolbar call paths, contains callback faults, and adds a local interop regression gate plus three core tests (66 total). See [GUI repair evidence](gui-rendering-repair.md). Native menu retesting remains required.

Gameplay release acceptance is blocked by manual native-UI/save testing that was not performed through available tools. The user explicitly authorized continuing implementation and documenting those gaps. Uncertain native hook tasks are closed with the required Incompatible fallback; supporting them later requires new native behavior evidence and the corresponding manual matrix. No unverified feature is claimed working and no v1 release-ready claim is made. The implementation branch, source, research and local candidate ZIP are preserved for review; no push, merge or publication was performed.
