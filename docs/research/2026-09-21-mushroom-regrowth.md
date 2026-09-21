# Mushroom regrowth: current native contract

Verified read-only against installed Steam build 25326768, Unity 6000.3.12f1,
on 2026-09-21. No game was launched and no save was edited. Raw assets,
disassembly, and inspection tools remain outside the repository.

## Positive resource identity

UnityPy read the shipped MonoBehaviour headers, MonoScript references and raw
serialized records in `ASKA_Data/sharedassets0.assets`. These are the three
`SSSGame.BiomeResourceInfo` resources, not inventory food or visual effects:

| Asset name | ItemInfo.id | Asset path ID |
| --- | --- | --- |
| Item_Food_BiomeMushroom1 | 0x01004008 | 136010 |
| Item_Food_BiomeMushroomGrey | 0x01004009 | 136011 |
| Item_Food_BiomeMushroomYellow | 0x0100400A | 136012 |

`ItemInfo` inherits `AssetNode`, which adds no serialized fields. The item ID
is the first serialized integer after the aligned MonoBehaviour name. The
three records also refer to the separate gathered items at path IDs
136056, 136058 and 136060. The shipped mushroom-gathering deed independently
references these three biome resource records and gathered item records.

Runtime qualification requires both exact ID and exact asset name, a
`BiomeResourceInfo` instance, and every associated biome descriptor referring
to that same native resource object. Names containing "mushroom" alone are
never sufficient. Cooked food, soup, shields, old Invector demonstration
assets and VFX do not qualify.

The mushroom resource records end with native replenishment data:
`replenishWhenAvailable = true`, empty `replenishSeasonStart`, and
`replenishFrequencyDays = -1`. Red/grey refer to `Mushroom_Replenish`
(path ID 139383); yellow refers to `MushroomYellow_Replenish` (139164).
Their serialized customization slots include availability-only, 2/3/4-day
periodic, seasonal, and disabled choices. Merely dividing a positive native
frequency would do nothing in the normal availability-only configuration.

## Verified scheduling and eligibility

The current native method map and x64 disassembly establish:

- `BiomeItemAvailabilityData.CalculateNewReplenishDate` at VA 0x180EA9BF0
  clears its dirty bit, obtains the resource's current `GetReplenishData`,
  calls `ReplenishData.GetNextReplenishTime(WeatherSystem.Instance)`, and
  stores the returned date.
- `ReplenishData.GetNextReplenishTime` at VA 0x180FEBB40 initializes a
  disabled date of -1. A positive frequency produces current networked game
  time plus frequency in game days. Season starts can replace it with an
  earlier future date. A season-only date must never be divided as though
  it were a periodic wait.
- `WeatherManager.HandleDescriptors` at VA 0x180F742F0 reads the manager's
  resource/availability dictionary and invokes `AvailabilityProcess.CheckAll`
  against its current weather summary. It manages the native available
  state, remaining lifetime, availability-transition replenishment and
  dirty scheduling.
- At 0x180F74579 it requires `CurrentDescriptorsState`. If dirty, it calls
  `CalculateNewReplenishDate`. At 0x180F74593 it accepts any nonnegative
  stored due date; at 0x180F745AE it compares the summary's absolute game
  time against that date. It does **not** test the resource frequency in
  this dispatch branch. Once due, it invokes the existing descriptors'
  virtual replenishment method and marks the next date dirty.
- `BiomeItemDescriptor.Replenish` at VA 0x180E95100 resets existing active
  biome instances and marks existing cell-owner descriptor data for native
  replenishment. This path is left entirely native. The feature creates no
  placements, calls no spawn API and changes no density or population cap.

## Implemented behavior

The only hook is a prefix on the no-argument `WeatherManager.HandleDescriptors`.
There is no primitive-by-reference target and no global `ReplenishData` hook.
The hosted single-player gate and initialized, authoritative weather system
are required before work. The active callback supplies the world manager;
world manager, weather clock, resource and availability-data native identities
are all part of deadline ownership.

Normal leaves schedules untouched. For a positive native periodic frequency,
2x/4x shorten the next deadline to no later than current game time plus
frequency/2 or frequency/4. A sooner existing native deadline, including a
season boundary, remains unchanged. Due/overdue native deadlines are left
alone. Season-only and never-replenish modes remain unchanged.

For the default availability-only mode, the explicit reference is two game
days: 2x schedules one game day and 4x half a game day. This reference matches
the shortest shipped periodic customization option; it is a trainer policy,
not a claim that native availability transitions occur every two days.
It requires native current availability and `CheckAll` to pass. If rain or
season eligibility stops passing, a still-owned shortened deadline is
restored before native dispatch, including during native lifetime grace.

Existing depleted mushroom resources are adopted on the next weather dispatch
with a clean native date. Dirty records are first recalculated by the game
and can be adopted on the following dispatch. No full availability transition
or new harvest is required to enable the feature. Each elapsed trainer
deadline is consumed by the native dispatcher, whose dirty bit ends ownership;
the game calculates the next native date before another interval is applied.
There is no catch-up loop after a clock jump.

Discovery is bounded to 512 resource entries and runs only when the manager or
resource-table count changes, or a cached key becomes stale. Ordinary callbacks
look up at most three cached resource keys and validate their live availability
records, with at most 32 descriptors per matching resource. Only the three
positively identified resources can receive schedules. Diagnostics log at most
eight applications.

Changing the multiplier restores a still-owned native date before deriving a
new one. Disable/reset restores only entries still in the same live manager,
clock and resource table whose current date exactly equals the applied date
and whose dirty bit is clear. Consumed, replaced or stale world records are
never written. Already-regrown mushrooms are normal native game state and
are not removed when the option is disabled.

## Validation and remaining gameplay checks

Pure tests cover exact identity allowlisting; availability-only 2x/4x cadence;
finite schedules; preservation of earlier seasons, overdue dates, normal,
disabled and season-only modes; invalid times; and cleanup refusal after
world, clock, resource, data, deadline or dirty-state changes. Root reported all
220 tests passing and an integrated plugin build with zero warnings or errors
on 2026-09-21. The later discovery-cache refinement requires the final integrated
build recorded by root.

Live gameplay remains untested. Acceptance should harvest an existing eligible
mushroom patch, enable 4x and wait half a game day during a qualifying weather
window, verify native replenishment, then disable and check future native
timing. Verify yellow mushrooms still require their native season, dry or
otherwise ineligible conditions do not trigger added replenishment, unrelated
berries/resources retain their original timing, and changing worlds/resetting
does not reuse old deadlines. Native save serialization may capture a pending
accelerated deadline while enabled, just as it captures ordinary due dates;
disable before saving when testing restoration across a restart. This has not
been tested in a live save.
