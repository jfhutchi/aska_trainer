# Item decay and equipment wear: native investigation

## 2026-09-23 replacement candidate

Version 0.1.20 adds player-only Infinite Durability through the by-value
`Property.SetValue(float)` boundary. Current ASKA build 25440748 dispatches tool
hit wear, periodic equipment decay, and digging wear through the virtual setter
at property vtable slot `0x188`. The base `Property`, `Attribute`, and
`VariableAttribute` setter implementations are all patched because the latter
two override it. The feature admits only the current player's carried, equipped,
or stowed `EquipmentItem` whose decay property is ID 1010. It blocks only finite
new values greater than the existing value. Repair and other decreases run
normally. A 250 ms pointer refresh avoids scanning the inventory for every
world-item decay update; a matching setter rechecks live ownership before
blocking. No item wrapper is retained between calls. The old by-reference
`Run` and `_DealDurabilityDamage` hooks remain absent. Native gameplay testing is
pending; no result is claimed for No Spoilage.

## 2026-09-21 status correction

Version 0.1.6 withdraws Infinite Durability and No Spoilage after the latest
0.1.5 log recorded 688,666 item-decay and 246 equipment-wear trampoline errors.
Both controls now reject activation and their hook implementations are removed.
Normal game wear/spoilage applies. Earlier implementation descriptions below
are historical, not current supported behavior. Replacement item protection is
outstanding. See [building and item-error evidence](../testing/0.1.6-building-retest.md).

Inspected the user's local ASKA Steam build 25186770, Unity 6000.3.12f1, with
LibCpp2IL metadata and Capstone disassembly on 2026-09-14. The raw game binary,
metadata, method map, and disassembly remain outside the repository. This evidence
establishes native arithmetic and call boundaries; gameplay acceptance is still
required.

## Correction to earlier freshness research

`SandSailorStudio.Inventory.ExpirationProcess.Run` is an availability-based
expiration path. It asks `AvailabilityProcess.CheckAll(WeatherEventData)`, records
the `c_Expirable` state when available, and removes an item/world instance when
availability expires. Its body does not contain a freshness interval increment.
Disabling it would suppress seasonal/world availability behavior. The trainer
does not patch it.

Actual item decay is handled by `SSSGame.ItemDurablilityProcess.Run(Item, ref float)`.
The current native body is at RVA `0xD8B9C0`. It obtains `c_Decay` (1010) and
`c_Durability` (1013), performs existing break/unbreakable handling, then selects
inventory/world/equipped processing by `DecayMode`. It reads item and container
decay rates/protection plus weather/customization multipliers. The final write
adds the computed nonnegative decay amount multiplied by the input interval to
the existing decay value. The interval is read only at this final multiplication;
the native function does not write it.

The shared trainer hook supplies zero for a positive finite interval during this
one call. It runs the native process, including its normal checks. It restores the
caller's interval in both postfix and exception finalizer so other item processes
do not inherit a zero interval. It does not refill durability/freshness or set an
item's decay value. Negative and invalid inputs are left unchanged.

`Infinite Durability (carried equipment)` selects `EquipmentItem` instances;
`No Spoilage (carried items)` selects other items. The latter includes decaying
carried resources, not only consumable food. Both require freshly resolved local
inventory ownership and native item authority. Equipment detached from the normal
collection is accepted only when it is currently equipped or stowed and its current
equipment manager matches the local character's manager. Previous inventory is not
used as ownership evidence. Ground items, settlement storage, and other characters'
inventory are excluded. No item/native wrapper is cached across calls.

One shared Harmony registration serves both toggles. Disabling one leaves it
installed for the other; disabling the final user removes it. This avoids two
identical patches overwriting the same Harmony per-call state.

## Direct wear on tool hits

`Character._DealDurabilityDamage(EquipmentItem, ref float durabilityDamage,
ref float durabilityRemaining)` reads durability and decay, computes
`min(durability, decay + durabilityDamage)`, writes decay, reports remaining
durability, and calls `Item.SyncDecay`. The current native body at RVA `0xB2A7E0`
is shared with the Creature and StructureDamageReceiver variants. The trainer
patches that native body once through Character. It examines only the supplied
item, not the receiver, and supplies zero positive wear while preserving native
remaining-durability output and sync. The caller's input is restored afterward.
This body-sharing observation is specific to the inspected build; hit-receiver
coverage must be rechecked after game updates.

`DiggingMeleeObject._DealDurabilityDamage()` is separate (RVA `0x126C4D0`). Its
native body only resolves the weapon's decay property and adds
`hitDurabilityDamage`. The trainer suppresses this narrowly isolated wear function
for a currently owned weapon; terrain modification remains native.

## Verification and remaining acceptance

Core tests exercise positive-input restoration, repeated postfix/finalizer
restoration, nested interception, and leaving negative/zero/nonfinite inputs
untouched. Plugin compilation checks all referenced current signatures. These
checks do not establish that IL2CPP detours are reached during actual gameplay.

Required disposable-save acceptance:

- Partly damaged axe: chop wood and hit a creature; decay stays at its current
  level, normal target damage and tool effects still occur. Repeat with pickaxe,
  shield/armor, and digging. Repair remains functional.
- Partly spoiled food/carried resource: wait several game hours, verify unchanged
  decay rather than a reset. Cooking/consumption still work normally.
- Enable both toggles, then disable each independently; the other keeps working.
  Disable both and verify natural wear/decay resumes.
- Equip, stow, unequip, drop, pick up, and transfer to storage. Protection follows
  current local ownership and ends outside it. Villager equipment is unaffected.
- Save/reload and world changes: no cached item handles, no errors, and no
  freshness/durability inflation. Existing already-broken or expired items are not
  repaired or resurrected.

No such gameplay test was performed by this worker. The features are implemented
development candidates, not gameplay-validated release claims.
