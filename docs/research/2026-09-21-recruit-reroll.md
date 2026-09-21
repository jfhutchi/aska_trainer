# Traits-only recruit rerolls

Inspected installed Steam build 25326768 on 2026-09-21. Native addresses below
are research references in GameAssembly.dll, not runtime offsets used by the
feature. Generated interop was inspected separately with Mono.Cecil.

## Native generation and compatibility

`VillagerOutlet.GenerateDescriptionData` at `0x180C060A0` generates definition,
increments the definition's last name index, shuffles the name, generates
appearance, generates perks, and chooses a portrait camera. Calling this and
copying selected fields would still advance unrelated name/appearance state.
The trainer therefore calls only its verified perk generation substep:

- Definition comes from `PopulationManager.villagerDefinitions[data.definitionID]`.
- The definition's `character` prefab provides `PerksManager`.
- At `0x180C062A5..0x180C062B6`, native calls
  `PerksManager.GetRandomPerks(5, out tables, null)`.
- At `0x180C062D6`, it constructs `Villager.PerksData(tables)` and assigns only
  the five perk slots. `PerksData` constructor at `0x180DBFA20` encodes each
  modifier table's ID and fills trailing absent entries with `-1`.

`PerksManager.GetRandomPerks` at `0x180E3BEE0` creates a new list, gathers tables
whose `effectType` equals the prefab manager's `PerkType`, randomly removes one
from the remaining pool per pick, and removes incompatible remaining perks.
The compatibility loop compares affinity identity and opposing affinity values.
It stops after five picks or when the compatible pool is exhausted. It does not
apply effects to the prefab or a live villager. A null random generator uses
Unity's ordinary random source, exactly as normal outlet generation does.

The implementation calls this native generator once per Preview action, then
checks 1..5 results, distinct nonnegative IDs, matching effect type, and native
`PerkModifierTable.IsCompatibleWith` for each applicable pair. It copies the
entire original description and changes only `perks`. Definition, name, all seven
appearance components and portrait-camera index are preserved exactly.
Native trait names and descriptions are obtained from `PerksData.ToStatusEffectList`
and table `Name`/`Description`; the trainer does not invent derived attribute values.

## Choice versus selection and summoning

`VillagerOutlet.GenerateNewVillagerChoices` at `0x180C06360` writes descriptions
to the `upcomingVillagerChoices` network array, then calls
`_TryChooseRandomLostVillager`. It must never be used for this feature.

The network array has a final slot reserved for a lost villager. Native ordinary
choices occupy `0..villagerChoicesCount-1`. The implementation additionally
requires `villagerChoicesCount >= 2` and strictly less than array length, and
never presents or writes the final slot. Golem definitions are excluded.

`SelectVillagerMenu.OnSetup` at `0x18121B500` calls `_ShowPanels`
(`0x18121BC80`). In the ordinary multi-choice branch (`nrOfChoices >= 2`),
that method reads each current network choice and creates its preview panel.
Its single-choice branch instead calls full `GenerateDescriptionData` and keeps
`_generatedSingleData`; special/single-choice recruitment is outside this feature.
`VillagerOutlet.OnStorageMenuConfirmationPressed` (`0x180C06D50`) sets
`nrOfChoices` to `villagerChoicesCount` for spawning configuration zero, and
one for other configurations.

`SelectVillagerMenu._OnConfirmButtonPressed` at `0x18121BA80` rereads the chosen
ordinary array slot and passes it to `Rpc_SetUpcomingVillagerData`. That RPC at
`0x180C07340` writes selected `upcomingVillagerData`, marks whether the final
lost-villager slot was chosen, and can remove that lost villager from population
storage. The trainer does not call this RPC or alter this selection state.

After normal confirmation, `VillagerOutlet._OnSelectVillagerConfirmed`
(`0x180C08790`) consumes the normal summoning container and sets
`activationInteraction.IsActive = true`. `Activate` (`0x180C057F0`) sets
`_SpawnPending` and computes the normal deadline. `SpawnVillager`
(`0x180C079C0`) passes selected `upcomingVillagerData` into normal
`PopulationManager.TryAddVillager`. Existing villagers are never targeted.

## Preview, apply and stale-state guards

All native reads, generation and writes happen only on explicit Refresh,
Reroll preview and Apply these traits actions, through `NativeActionFeature.RunOnce`.
Enable/disable and restored configuration cannot replay a reroll. The panel draws
cached managed strings; it does not search the world during GUI layout or repaint.

Each action refreshes the single-player guard and resolves the current population
and settlement. It reads the population manager's registered outlet list, bounded
to 128 entries, and accepts only active, intact settlement-owned structures with
valid authoritative network objects and a master session. The outlet must have
no pending spawn, no active summon, no selected activation interaction and no
chosen lost villager. An open native recruit menu blocks these actions; the user
closes it first and reopens it after Apply. Native OnSetup then rebuilds the preview
from the modified array. No stale native preview remains visible across a write.

Cached identity includes current population, settlement and outlet instance IDs,
choice slot and count, definition/name IDs, every appearance component (float bit
patterns), portrait camera and all original perks. Reroll and Apply resolve the
outlet anew and compare the full identity. Apply does one native
`NetworkArray<DescriptionData>.Set` on the verified ordinary slot, then reads it
back and confirms every field. It does not retain native object references between
actions. Refresh, explicit reset and panel clear discard staging; a failed Apply
also clears it so the user must refresh rather than replay a possibly stale write.

## Persistence and validation limits

`VillagerOutlet.Serialize` at `0x180C07680` saves selected `upcomingVillagerData`
(at `0x180C077F9`) plus pending/used/deadline and optional lost-villager state. It
does not serialize the array of unchosen choices. Rerolled choices remain subject
to ordinary regeneration on reload or game reroll. The UI explicitly tells the
user to review and choose the candidate through the normal screen before relying
on it. The trainer does not add custom save data or promise unchosen-choice
persistence. Once the player confirms the ordinary choice, native selected-data
serialization and normal spawning use that same modified description.

All 12 filtered `RecruitChoiceStampTests` passed on 2026-09-21. Pure tests cover candidate invalidation by world/outlet/slot/count/identity/perk
changes, pending/active/selected/lost-state rejection, and native perk sentinel,
duplicate, gap and invalid-ID handling. Build/test results are recorded by the
integrating agent after the full feature set is compiled.

Not yet gameplay-verified: actual generated trait display/localization, preview
matching the summoned villager, stale-selection rejection during a summon,
normal resource consumption, and selected recruit save/reload. No game launch,
save mutation, actual summon or achievement modification was performed during
this research. Any runtime failure remains subject to the existing hosted-feature
circuit breaker.
