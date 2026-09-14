# HutchASKA Implementation Plan Index

These plans are intended to be executed in order by **local Codex on the Windows machine that has ASKA installed**. The local environment is authoritative because the proprietary/generated ASKA interop assemblies are intentionally not stored in GitHub.

## Execution order

1. [`2026-09-14-hutchaska-stage-1-foundation.md`](2026-09-14-hutchaska-stage-1-foundation.md) — repository hygiene, MIT license, README, changelog, pure core, tests, local plugin project, build/install scripts, single-player guard contract, diagnostics shell, CI.
2. [`2026-09-14-hutchaska-stage-2-player-world.md`](2026-09-14-hutchaska-stage-2-player-world.md) — God Mode, stamina, survival, movement, world time/game speed, Player/World UI and hotkeys.
3. [`2026-09-14-hutchaska-stage-3-items-crafting.md`](2026-09-14-hutchaska-stage-3-items-crafting.md) — durability, freshness, retain-on-use, item browser/give flow, crafting, building, repairs.
4. [`2026-09-14-hutchaska-stage-4-tribe-release.md`](2026-09-14-hutchaska-stage-4-tribe-release.md) — global tribe controls, individual villager editor, instant normal recruitment, final diagnostics, packaging, README/license audit, release validation.

## Required source documents

Before changing code, read:

- `docs/superpowers/specs/2026-09-14-hutchaska-trainer-design.md`
- `docs/research/2026-09-14-aska-interop-observations.md`
- the current stage plan in full.

Later research files created by earlier stages become authoritative inputs for later stages.

## Local Codex rules

- Work in a dedicated feature branch/worktree, not directly on `main`.
- Use the local ASKA installation to inspect exact current IL2CPP signatures whenever a plan says to verify a member.
- Set `ASKA_GAME_DIR` to the local ASKA root before the plugin build, or make the build helper discover it only when discovery is unambiguous.
- Never copy ASKA DLLs, BepInEx generated interop assemblies, game assets, or user-specific paths into Git.
- Never invent an ASKA method/property name to satisfy a compiler error. Inspect the current local assembly and update the research notes.
- Execute tasks in order. Each task has its own test/build/commit gate.
- Do not claim an in-game feature is working until the specified local smoke test has been performed.
- If a feature cannot be safely verified, mark only that feature `Incompatible`/disabled and continue.
- Unknown session mode is not permission: gameplay-changing features remain blocked until single-player/local play is positively confirmed.
- Do not touch Steam achievement APIs.
- Do not use fixed process offsets or pointer chains in v1.
- Keep the public repository and release ZIP free of proprietary/game binaries.

## Documentation and license requirements

The project must contain and maintain:

- a complete root `README.md` with status, compatibility, features, requirements, install, source-build, local-reference, controls, safety, achievements, update, diagnostics, limitations, contributing, license, and third-party/game-assets sections;
- a standard MIT `LICENSE` for HutchASKA-authored source with `Copyright (c) 2026 jfhutchi`;
- `CHANGELOG.md`;
- `THIRD_PARTY_NOTICES.md` before v1 release;
- exact tested ASKA/Unity/BepInEx versions in release validation documentation.

## Completion rule

Do not jump to a later stage to make the UI look complete. Finish the current stage's automated tests, local build, relevant in-game smoke tests, documentation update, and commit before continuing.
