# Stage 4 tribe and diagnostics verification

Date: 2026-09-14. Tasks 1-6 implementation and local build are complete; gameplay acceptance remains **MANUAL VERIFICATION REQUIRED**. This worker did not install the plugin, launch ASKA or load/edit a save. Packaging/release verification belongs to the coordinating agent.

Subsequent coordinating-agent package installation and combined startup passed; see the [final checklist](v1-release-checklist.md). The task-local evidence below is historical; native UI and gameplay acceptance remain manual.

## Automated evidence

- Full Release core suite: 63 tests pass, including nine tribe DTO/search/need-request tests and eight compatibility-refresh/reset/Steam-manifest tests added in this stage.
- Tests were written before the corresponding pure implementations; missing contract/helper failures were observed before green runs.
- Local Release plugin build: zero warnings and errors against the current installed interop assemblies.
- Native references were inspected with local Mono.Cecil. See [tribe-api-map.md](../research/tribe-api-map.md) for exact members and unresolved semantics.
- Raw villager types, casts, population enumeration, attribute access and damage-target discovery stay inside AskaTribeContext. UI/services hold primitive snapshots and GUIDs only.

## Manual acceptance matrix

| Check | Result |
| --- | --- |
| Current owned, living villagers listed; enemies, guests and unrelated populations excluded | MANUAL VERIFICATION REQUIRED |
| Invincibility prevents owned villager damage; disabling restores native damage; player/enemies unaffected | MANUAL VERIFICATION REQUIRED |
| Food, water, energy, rest and happiness independent; each native maximum/current happiness cap respected | MANUAL VERIFICATION REQUIRED |
| New registered recruits receive active maintenance on the next half-second pass | MANUAL VERIFICATION REQUIRED |
| Heal Entire Tribe and Restore All Needs are one-shot; latter leaves health/warmth/lifetime alone | MANUAL VERIFICATION REQUIRED |
| Individual selection shows name and GUID; only changed fields apply to a freshly resolved current villager | MANUAL VERIFICATION REQUIRED |
| Query/page changes clear selection; disappearing/dead villagers clear stale editor state and show an error | MANUAL VERIFICATION REQUIRED |
| Temperature immunity, freeze aging and instant recruitment show Incompatible reasons without mutating native state | Implemented fail-closed; runtime UI acceptance pending |
| Warmth is read-only; Age unavailable; remaining lifetime is never mislabeled as chronological age | Implemented fail-closed; runtime UI acceptance pending |
| Reset All stops gameplay features, restores native movement/time/game speed where possible, resets both controller multipliers to 1x and clears item/tribe editor state | Core faulted-controller reset test passes; native restoration pending |
| F8 and diagnostics remain available after reset; visible menu input protection is maintained | MANUAL VERIFICATION REQUIRED |
| Reload disables features first, refuses pending native cleanup and does not enable features from reloaded configuration | Implemented; runtime acceptance pending |
| Re-scan disables before re-probing and cannot duplicate patches or clear terminal runtime faults | Pure lifecycle tests pass; native Harmony acceptance pending |
| Diagnostics shows application version separately from a matching Steam manifest build ID, actual Unity/BepInEx versions, every feature state/reason and pending cleanup | Parser tests pass; loader/UI acceptance pending |
| Errors always reach BepInEx/LogOutput.log; Verbose adds lifecycle/rescan information | MANUAL VERIFICATION REQUIRED |
| Scene/save reload does not reuse a villager wrapper; native field failures isolate the owning feature | Source boundary verified; native fault/scene tests pending |

Expected loading/missing-membership conditions publish a concise adapter reason without consuming native error budgets. Unexpected native field failures propagate to the particular hosted feature/action breaker. Only shared population-discovery failures can latch the shared adapter after three failures; restart is required after that terminal fault. Rescanning intentionally does not remove this fault policy.

Steam build detection uses the actual BepInEx game-root path and its Steam library manifest, verifies the installation directory, and reports Unavailable on missing/mismatched data. It never substitutes Application.version for a Steam build ID. Runtime compatibility is not established by this metadata.
