# Instant normal recruitment native evidence

Target: ASKA Steam build 25186770, Unity 6000.3.12f1, IL2CPP metadata 39.
Read-only LibCpp2IL metadata and Capstone disassembly of the installed game
establish the chain below. Addresses are evidence locations, never runtime
patch offsets. No game/save files were changed during this investigation.

## Native trigger, deadline and completion

1. `_OnSelectVillagerConfirmed` (0x180C074F0) removes the normal selected
   configuration's required items through `ItemContainer.RemoveAllItems` and
   activates the native activation interaction. The trainer does not intercept
   selection, availability, cost checks, description generation, or this method.
2. `VillagerOutlet.Activate` (0x180C04550) requires an inactive outlet and the
   master session. It sets `_WasUsed` and `_SpawnPending` true, then assigns
   `_NetworkedVillagerTimerEnd`. Its duration comes from
   `_spawnTimeline.cooldowns.GetValue(populationCount)`, divided by 24 and
   multiplied by gametime customization and inverse custom day-length
   multiplier. It adds the result to `WeatherSystem.NetworkedCurrentGameTime`.
   The divisor was read from the binary and confirmed as float 24.0.
3. `_OnWeatherChanged` (0x180C077C0) starts by reading that outlet's
   `_NetworkedVillagerTimerEnd`. It computes
   `max(deadline - (weather.dayOfYear + weather.timeOfDay / 24), 0)`.
   While pending, it emits normal activation/progress events. At zero remaining
   time, and only on the master session, it calls `SpawnVillager()` then clears
   `_SpawnPending`. An idle outlet therefore does not create another villager.
4. `SpawnVillager` (0x180C06720) passes the native selected upcoming description
   and position to `PopulationManager.TryAddVillager`. It handles the native
   lost-villager restoration branch, calls `GenerateNewVillagerChoices`, invokes
   the arrival event and records native analytics. `TryAddVillager` reaches
   the game's existing `CreateVillager` path. These functions are not replaced.

## Narrow interception

The feature patches the named `_OnWeatherChanged` callback and the named
deadline getter. Its thread-local scope identifies only the current outlet,
and only after the hosted single-player gate, live valid network object/state
authority, master session, pending summon, and active/valid/living owning
structure checks pass. Its owning structure's native `Settlement` must match the
fresh uniquely resolved current settlement; missing or ambiguous settlement
discovery fails closed.

For the first deadline read in this callback, the getter returns
`min(nativeDeadline, currentNativeDay)`. This makes the normal completion test
due. The scope is consumed immediately on that read. Later events and
serialization, including events fired synchronously during recruitment, see
the unmodified native deadline. An exception-preserving Harmony finalizer
restores any outer callback scope, including when the callback throws.

The trainer writes neither deadline nor pending state and never calls
`SpawnVillager`, `TryAddVillager`, a villager constructor, or a global generic
threshold hook. It holds no outlet across frames. Disabling removes its own
patches. A pending summon retains its native timer if no completion callback
has run; a completed normal recruit is a world change and cannot be undone by
disabling the toggle.

## Persistence and validation limits

Native `Serialize` reads the deadline and pending state along with upcoming
villager data; `Deserialize` restores the deadline/pending state and selected
description. The one-read scope prevents the trainer's temporary return value
from leaking into these later reads. No serializer or save hooks are installed.

This is evidence for preserving the existing lifecycle, not a completed
in-game acceptance test. Manual checks remain: two separately confirmed
summons with normal costs, expected names/traits, normal AI and settlement
membership, no repeated spawning from an idle outlet, disable before a pending
summon finishes, destruction/unload, and save/reload after completion. Existing
pending normal summons also complete at their next native weather callback
while enabled. If that callback is paused or not running, this feature does
not manufacture one or change world time.

The installed SummonTimeModifier's broad `IntThresholdList<float>.GetValue`
postfix was not copied. It was a research lead; the implemented feature has no
generic-list interception and does not alter another system's cooldown.
