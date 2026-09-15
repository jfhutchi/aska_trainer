# Changelog

Notable changes to HutchASKA are documented here, grouped by release and change type.

## [Unreleased]

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
