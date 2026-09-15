# Tribe adapter API map

Inspected local build 25186770 with Mono.Cecil on 2026-09-14. Signatures and a successful build are not gameplay acceptance.

## Membership and identity

`SSSGame.PopulationManager.GetPopulation()` and `GetAllVillagers()` return native lists of `SSSGame.Villager`; `IsLoading` is bool. The adapter uses the registered population, requires one current population manager and settlement, a positively confirmed single-player session, a current local player, matching `Villager.teamId` / `PlayerCharacter.TeamId`, native authority, living state, and matching `Villager.GetSettlement()`. A populated `_guestStation` excludes guests conservatively. The meaning of `IsViking` is not established, so its name is not used to infer outsider status. If ownership is ambiguous the adapter exposes no target. These membership semantics still require a disposable-save check with owned and visiting characters.

`Villager.GetGuid()` returns string; `PersistentUniqueID` is int. The adapter uses a nonempty GUID and excludes duplicate GUIDs. It never stores a native pointer as identity. All villager casts, list enumeration, snapshots, writes and damage-target discovery stay inside `AskaTribeContext`; no raw villager survives a public adapter call.

Expected loading, missing targets and validation failures return empty/error results without spending the native error budget. A primitive status interface exposes the reason. Unexpected native snapshot/edit/callback failures propagate to each caller's hosted breaker, isolating unrelated features. Only the shared population-discovery path latches the adapter after three errors; a latched adapter performs no further population access until restart. Global maintenance runs at most twice per second per feature and enumerates fresh current IDs, so newly registered members enter the next pass. Restore All Needs excludes health, warmth and lifetime; Heal Entire Tribe is a separate one-shot action.

## Fields

`Villager.GetSurvival()` returns `VillagerSurvival`, inheriting `CharacterSurvival`. The latter exposes VariableAttribute `_foodVAttr`, `_waterVAttr`, `_energyVAttr`, `_warmthVAttr`; VillagerSurvival adds `_restVariableAttribute` and `_lifetimeVariableAttribute`. Character exposes `_healthVAttr`, float `MaxHealth`, bool `IsDead`, bool `HasAuthority`; Villager exposes `_happinessVAttr`, float `HappinessCap` and string `GetName()`.

`SandSailorStudio.Attributes.VariableAttribute` has float `min`, `max`, `GetValue()` and void `SetValue(float)`. Requested fractions are finite, clamped to [0,1], then mapped to native ranges. Health is capped by MaxHealth and happiness by HappinessCap. All requested ranges are validated before writes. Null request fields make no edit. Native setter failures may leave earlier requested fields applied; no speculative rollback is attempted.

Null fields also skip the native getters/cap access entirely, so a food-only maintenance pass does not touch health or happiness. Snapshot refresh intentionally reads all visible fields; Apply Changes sends only fields changed in the current editor draft. Failed multi-field/global edits surface a partial-update notice and require a fresh inspection rather than retrying automatically.

Warmth's safe endpoint is unverified, so it can be read but cannot be edited or maximized. Remaining lifetime is not chronological age: `_OnLifetimeReachedMin()` is a native death/progression callback, not a safe age setter. Snapshot Age is null; age editing and freeze aging are unavailable.

## Damage and recruitment

`void SSSGame.Villager.TakeDamage(SSSGame.Combat.DamageData)` is declared on Villager. The proposed narrow interception must check current owned membership and the hosted single-player decision before suppressing that call.

The implemented prefix targets only this declared overload. It receives an opaque object, delegates all casts and current membership checks to the adapter, and defaults to native damage if gating or identification fails. Its own Harmony ID is removed on disable. It never writes maximum health. Actual damage interception and disable behavior require a disposable-save test; no runtime pass is claimed.

`VillagerOutlet` has float `gametimeToSpawnVillager`, a trial-time parameter reference, float `_NetworkedVillagerTimerEnd`, and Fusion NetworkBool `_SpawnPending`. Candidate native methods include `OnStorageMenuConfirmationPressed(ItemContainer)`, `_OnSelectVillagerConfirmed(ConfirmActionMenu)`, bool `_CheckSummonAvailability(ItemContainer)` and `SpawnVillager()`. Generated native-invoke wrappers do not establish timer units, rearm order, or a complete normal recruitment lifecycle. No timer write, constructor, spawn call or recruitment patch is justified from this evidence alone.

The subsequent native investigation in [instant-recruitment.md](instant-recruitment.md) established the trigger, day-based deadline, normal completion, pending-state clear and native next-choice generation. Instant Normal Recruitment now overrides one deadline getter result only within an owned pending outlet's native weather callback. It does not write a timer or spawn directly. Two normal recruits, no runaway repeat, native AI and save/reload remain MANUAL VERIFICATION REQUIRED; no recruitment/save test was performed by this worker.
