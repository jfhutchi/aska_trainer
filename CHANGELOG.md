# Changelog

Notable changes to HutchASKA are documented here, grouped by release and change type.

## [Unreleased]

### Changed in 0.1.7 development candidate

- Added a Fishing & Foraging tab with normal/2x/4x bite-speed and eligible rare-fish weighting controls, plus Easy Catch. Casting, reeling, bait, loot and fishing progress continue through the native game actions.
- Added mushroom-only regrowth controls. Finite native intervals are divided by the preset. Default weather-driven mushrooms use a two-day reference (2x: one game day; 4x: half a game day), retaining native availability checks. Season-only and never-regrow modes remain native.
- Added explicit upcoming-recruit trait previews and application through the Tribe panel, preserving candidate identity and appearance. Existing villagers are outside this action's scope.
- New controls default off/normal and participate in Reset All and configuration reload. Manual recruit rerolls are never replayed automatically.
- These are development features requiring gameplay and achievement-progress acceptance; no platform achievement or progress-counter hooks were added. The prior construction retest and unavailable features remain documented separately.

### Changed in 0.1.6 development candidate

- Recorded user-confirmed movement speed in 0.1.5, corroborated by native speed samples of 5 to 10 at 2x.
- Replaced the Build Speed work-amount hook after repeated native-to-managed exceptions stopped construction at zero progress. Normal native work submission is no longer patched.
- Scoped private copies of building configuration/moveset scale only the two work coefficients during one local hammer event. Shared assets, native elapsed time, tool wear, stamina, injury and proficiency settings remain unchanged.
- Added bounded building-event/progress logging, 14 coefficient regression cases and a local compiled-hook gate that rejects the withdrawn implementation.
- Withdrew Infinite Durability and No Spoilage after the latest 0.1.5 log contained 688,666 item Run trampoline errors and 246 equipment-wear trampoline errors. Their hook classes are absent and activation is rejected, including restored configuration. Normal wear/spoilage resumes; this is containment, not a completed replacement for those features.
- Re-inspected the replacement building path against updated ASKA Steam build 25326768 / game 1.44.1509261752 and added compiled regression checks for the withdrawn item hooks.
- Expanded leveling remains unavailable after its preview overflow; this update does not re-enable it.

### Changed in 0.1.5 development candidate

- Corrected movement's inverted native player-input permission check. Actual 0.1.4 diagnostics showed normal local commands were all rejected; native PlayerDrive requires hasExternalControl=true, while target matching clears it. Added regression cases for normal input and scripted/special movement exclusions.
- Corrected Build Speed's ambiguous Agent property lookup using declared session properties; the base session exposes another Agent property with a different return type.
- Withdrew expanded terrain leveling after a native crash while drawing the second side of a 20-tile preview. Removed the placement hooks and preset buttons; the remaining control explains that normal leveling must be used. 10x10 was not attempted and smaller expanded sizes are not claimed safe.
- Recorded user-confirmed Instant Summon behavior in 0.1.3 and actual 0.1.4 axe-harvest animation application. Movement and Build Speed need a new gameplay check.

### Changed in 0.1.4 development candidate

- Corrected movement's unhandled velocity path using a private, temporary movement-settings copy; retained complementary horizontal animation movement and native slopes/platforms/collisions.
- Corrected tool harvesting: shipped axe/pickaxe movesets allow melee while harvesting, so the old blanket rejection excluded normal tools. Match the active harvest action and restore animation speed before combat.
- Added bounded movement/harvesting diagnostics to identify actual hook dispatch and eligibility during the next gameplay test.
- Implemented Free Building through one-read supply checks while retaining real inventory, native work, layers and completion; disabling rechecks actual readiness.
- Implemented Free Repairs through native material eligibility, retaining normal hammer work. Deposited supplies stay committed and granted repair work is not revoked on disable.
- Added construction work presets 1x/2x/3x/4x, independent of Free Building.
- Added terrain-leveling presets 5x5/10x10/15x15/20x20 tiles using the larger native grid where required, with per-axis and storage-capacity limits. Reopen the leveling preview after changing size.
- Added Infinite Fuel for owned campfires and standing torches. Normal initial fuel, ignition, extinguishing and refueling remain required.
- New native effects require in-game acceptance; the user's reported failures in 0.1.3 are recorded in the follow-up retest.

### Changed in 0.1.3 development candidate

- Reworked the ineffective movement control after native tracing showed the old attribute controls animation input rather than physical displacement. The new local-player hook scales only new horizontal on-foot root motion before native collision handling.
- Added player harvesting presets 1x (normal), 2x, 3x and 4x, with separate native gathering and scoped tool-animation paths. Gameplay speed/effect acceptance remains pending.
- Moved Close to a fixed footer outside scroll layout and retained the close request until an explicit reopen. F8 and tab clicks were user-confirmed; the new footer requires a native click retest.
- Added local player and current tribe protection against further cooling/frost accumulation, preserving normal recovery. Existing cold/frost is not erased.
- Implemented carried-item spoilage protection and carried/equipped tool durability protection using the inspected decay, hit-wear and digging-wear paths. Seasonal availability expiration remains native.
- Implemented instant normal recruitment through a one-read deadline override inside the owned pending outlet's native completion callback, preserving normal population creation and rearm.
- Combined enabled tribe needs into one shared half-second pass and bulk snapshots into one population resolution; shared failures reach all participating toggles. Individual edits remain available.
- Cache sorted item/villager search results until data or query changes; reuse immutable feature snapshots and callback guards during updates/redraws.
- Added Ignore Crafting Materials for native local-player crafting through its temporary material manifest, retaining normal unlocks, blueprint costs and completion.
- Added Retain Consumables On Use after the native effect callback, preserving last-stack items without blocking unrelated inventory spending.
- Enabled -1/+1 hour through the verified native clock setter; backward midnight crossing is visibly rejected rather than wrapping to the wrong day. Clock adjustment preserves freeze state and does not simulate elapsed work.
- Documented native evidence, supported scope and remaining implementation blockers. New native behavior still requires the user's gameplay retest.

### Fixed in 0.1.2 development candidate

- Opaque trainer window, stronger text contrast, larger controls and selected-tab styling; preserve other GUI users' skin/tint/enabled state after drawing.
- Replace repeated manager discovery in session checks and stamina callbacks with lifecycle-invalidated candidate indexes that still check live state and uniqueness on each lookup.
- Avoid idle hotkey session refreshes and redundant survival writes. Add three discovery regressions (69 core tests total); actual FPS and visual acceptance remain pending.

### Fixed in 0.1.1 development candidate

- Replaced the tab toolbar with buttons because both the string-array conversion and final toolbar implementation are unstripping-failure stubs in the installed ASKA interop.
- Contained rendering exceptions inside the native window callback and outer draw call. The first fault is logged, the menu closes through its normal input/cursor restoration path, and rendering stays disabled until restart.
- Added a local managed IMGUI call-path check to build/install/package helpers and three callback containment regressions (66 core tests total). Native F8/tab/input retesting remains required.

### Added

- Current-owned-villager adapter, GUID snapshots/search, independent tribe needs controls, narrow villager damage interception, one-shot healing/restoration, and a changed-field-only villager editor.
- Advanced configuration reload, compatibility rescan, logging verbosity, actual Steam build detection, and Reset All handling for pending native/input cleanup.
- Clean release packaging of both authored assemblies and verified dependency-license notices; ZIP allowlist/identity checks and builds without local debug paths.
- 63 passing core tests at final automated verification, plus stage-specific and full release acceptance checklists.

- Runtime item catalog, pure search, paginated Items UI and guarded native definition-based Give Item/Give Stack with capacity and actual-quantity checks.
- Separate disabled durability, freshness, retention, crafting, building and repair controls with precise compatibility reasons and native API research.
- Stage 2 input transition/partial-context recovery fixes and regressions; 46 core tests pass at Stage 3.

- Stage 2 local player/world adapters with fresh, fail-closed single-player decisions before actions and Harmony callbacks.
- Player God Mode and three local stamina-drain hooks; independent food/water maintenance using native maxima.
- Reversible movement modifier, world-time freeze, and global speed presets with native pause/baseline handling.
- Player/World UI, configurable F1/F2/F5/F8 controls, owned menu input context, cursor restoration, Reset All, and opt-in state persistence.
- Core coverage for callback gating/fault isolation, normalization, multipliers, time wrapping, pause behavior and explicit cleanup retry.

### Known Limitations

- Temperature, +/-1 hour, durability/freshness/retention, free craft/build/repair, aging and instant normal recruitment report Incompatible because narrow safe native behavior is unverified. Warmth is read-only and age is unavailable.
- Plugin startup is observed; gameplay, input, native restoration and save persistence are MANUAL VERIFICATION REQUIRED. The local ZIP is a development candidate, not a validated v1 release.

### Foundation

- Project design and implementation planning.
- MIT licensing and public repository documentation.
- Local dependency policy and repository ignore rules for build output, generated interop, and private settings.

No runtime-compatible trainer release has been verified at this milestone.
