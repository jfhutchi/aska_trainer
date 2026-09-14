# Changelog

Notable changes to HutchASKA are documented here, grouped by release and change type.

## [Unreleased]

### Added

- Stage 2 local player/world adapters with fresh, fail-closed single-player decisions before actions and Harmony callbacks.
- Player God Mode and three local stamina-drain hooks; independent food/water maintenance using native maxima.
- Reversible movement modifier, world-time freeze, and global speed presets with native pause/baseline handling.
- Player/World UI, configurable F1/F2/F5/F8 controls, owned menu input context, cursor restoration, Reset All, and opt-in state persistence.
- 38 core tests, including callback gating/fault isolation, normalization, multipliers, time wrapping, pause behavior, and explicit cleanup retry.

### Known Limitations

- Temperature Immunity and +/-1 hour report Incompatible because safe warmth targets and native time units/day-boundary behavior are unverified.
- Stage 1 bootstrap is observed; Stage 2 gameplay and input smoke checks are MANUAL VERIFICATION REQUIRED. Compilation and core tests do not prove gameplay behavior.

### Foundation

- Project design and implementation planning.
- MIT licensing and public repository documentation.
- Local dependency policy and repository ignore rules for build output, generated interop, and private settings.

No runtime-compatible trainer release has been verified at this milestone.
