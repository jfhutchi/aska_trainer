# Changelog

Notable changes to HutchASKA are documented here, grouped by release and change type.

## [Unreleased]

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
