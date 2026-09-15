# Completing the currently unavailable options

Audit: 2026-09-14, ASKA build 25186770.

The initial audit found eleven explicit unfinished implementations whose probes unconditionally returned Incompatible. Version 0.1.3 replaces several with narrow native hooks. Implemented means source/build support, not a passed gameplay test; the remaining unconditional fallbacks cannot be enabled by rescanning or reinstalling.

| Group | Implementation route | Acceptance needed |
| --- | --- | --- |
| Player / tribe temperature | IMPLEMENTED: negative warmth rate and positive frost accumulation suppressed through verified callbacks; recovery preserved. Does not cure existing freezing. [Evidence](temperature-aging-native-audit.md) | Existing cold, entering cold/water, villager behavior, independent needs, disable |
| Durability | IMPLEMENTED: carried/equipped equipment's inspected decay/hit/digging wear; existing condition preserved. [Evidence](2026-09-14-item-decay-native-flow.md) | Partially worn tool, repairs, normal wear after disable, stored/dropped items, save/reload |
| Spoilage | IMPLEMENTED: carried non-equipment decay interval. Seasonal availability ExpirationProcess left intact. [Evidence](2026-09-14-item-decay-native-flow.md) | Partial freshness retained, decay resumes after disable, natural expiration and storage/save behavior |
| Instant normal recruitment | IMPLEMENTED: one deadline read inside the authoritative, current-settlement pending outlet's native completion. Original saved deadline and normal creation/rearm preserved. [Evidence](instant-recruitment.md) | Two independent summons, correct costs/traits/AI, no repeated spawning, pending summons, disable and save/reload |
| +/-1 hour | IMPLEMENTED: native SetGameTime with hour units and readback. Forward midnight advances natively; backward midnight is rejected because native negative wrapping does not rewind the day. | Same-day steps, forward midnight, visible backward rejection, day/events, freeze and persistence |
| Freeze aging | STILL UNAVAILABLE: native trace shows the inspected lifetime modifier is initialized only for golems. A general villager aging hook has not been established. [Evidence](temperature-aging-native-audit.md) | First establish ordinary villager aging; then other needs, recruits, disable and old-age boundary |
| Retain items on use | IMPLEMENTED: current local consumable use arms after native effects and suppresses one matching container decrement before last-stack detachment. Non-consumable spending is unchanged. | Food/drink/other usable item, last-item reuse, normal crafting/building consumption, disable and persistence |
| Free crafting | IMPLEMENTED: clears only the temporary material manifest in two exact local-player callers, preserving native unlocks, blueprint costs and product creation. [Evidence](2026-09-14-free-crafting-native-flow.md) | Zero materials, locked recipes remain locked, cancellation, normal consumption after disable and persistence |
| Free construction | IMPLEMENTED in 0.1.4: one-read empty supply result in exact part checks; real manifest and native work/layers/completion preserved. Disable restores owned readiness flags and recomputes actual eligibility. [Evidence](2026-09-14-free-building-native-flow.md) | Empty/partial supply, cancellation, completed structure registration, disable and persistence |
| Free repairs | IMPLEMENTED in 0.1.4: one exact fill-ratio read grants native repair work; normal hammer/healing path retained. Deposited materials stay committed and granted work is not revoked on disable. [Evidence](2026-09-14-free-repairs-native-flow.md) | Zero-material repairs, independence from crafting/building, next cycle after disable and persistence |

## Installed recruitment-mod evidence

Read-only inspection of SummonTimeModifier shows a postfix on IntThresholdList<float>.GetValue(int) that multiplies and clamps the result to at least one. It has no instance check, so its scope is broader than a verified recruitment-only hook. The current VillagerOutlet._spawnTimeline.cooldowns has that list type, giving a concrete lead for a narrower implementation. Free Villagers changes summon availability/confirmation, which does not establish timer completion or rearm behavior. These mods are evidence to investigate, not proof of safe trainer compatibility; this audit changes neither installed mod.

Temperature, durability/freshness, recruitment, consumption/crafting and now building/repair have native implementation evidence and an explicit acceptance matrix. Freeze Aging is the remaining unconditional fallback. No unavailable feature is silently enabled by this roadmap.
