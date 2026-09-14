# Tribe adapter API map

Inspected local build 25186770 with Mono.Cecil on 2026-09-14. Signatures and a successful build are not gameplay acceptance.

## Membership and identity

`SSSGame.PopulationManager.GetPopulation()` and `GetAllVillagers()` return native lists of `SSSGame.Villager`; `IsLoading` is bool. The adapter uses the registered population, requires one current population manager and settlement, a positively confirmed single-player session, a current local player, matching `Villager.teamId` / `PlayerCharacter.TeamId`, native authority, living state, and matching `Villager.GetSettlement()`. `Villager.IsViking` and a populated `_guestStation` exclude visitors/outsiders conservatively. If ownership is ambiguous the adapter exposes no target. These membership semantics still require a disposable-save check with owned and visiting characters.

`Villager.GetGuid()` returns string; `PersistentUniqueID` is int. The adapter uses a nonempty GUID and excludes duplicate GUIDs. It never stores a native pointer as identity. All villager casts, list enumeration, snapshots, writes and damage-target discovery stay inside `AskaTribeContext`; no raw villager survives a public adapter call.

## Fields

`Villager.GetSurvival()` returns `VillagerSurvival`, inheriting `CharacterSurvival`. The latter exposes VariableAttribute `_foodVAttr`, `_waterVAttr`, `_energyVAttr`, `_warmthVAttr`; VillagerSurvival adds `_restVariableAttribute` and `_lifetimeVariableAttribute`. Character exposes `_healthVAttr`, float `MaxHealth`, bool `IsDead`, bool `HasAuthority`; Villager exposes `_happinessVAttr`, float `HappinessCap` and string `GetName()`.

`SandSailorStudio.Attributes.VariableAttribute` has float `min`, `max`, `GetValue()` and void `SetValue(float)`. Requested fractions are finite, clamped to [0,1], then mapped to native ranges. Health is capped by MaxHealth and happiness by HappinessCap. All requested ranges are validated before writes. Null request fields make no edit. Native setter failures may leave earlier requested fields applied; no speculative rollback is attempted.

Warmth's safe endpoint is unverified, so it can be read but cannot be edited or maximized. Remaining lifetime is not chronological age: `_OnLifetimeReachedMin()` is a native death/progression callback, not a safe age setter. Snapshot Age is null; age editing and freeze aging are unavailable.

## Damage and recruitment

`void SSSGame.Villager.TakeDamage(SSSGame.Combat.DamageData)` is declared on Villager. The proposed narrow interception must check current owned membership and the hosted single-player decision before suppressing that call.

`VillagerOutlet` has float `gametimeToSpawnVillager`, a trial-time parameter reference, float `_NetworkedVillagerTimerEnd`, and Fusion NetworkBool `_SpawnPending`. Candidate native methods include `OnStorageMenuConfirmationPressed(ItemContainer)`, `_OnSelectVillagerConfirmed(ConfirmActionMenu)`, bool `_CheckSummonAvailability(ItemContainer)` and `SpawnVillager()`. Generated native-invoke wrappers do not establish timer units, rearm order, or a complete normal recruitment lifecycle. No timer write, constructor, spawn call or recruitment patch is justified from this evidence alone.
