# Fishing assists: current native contract

Inspected installed Steam build **25326768** on 2026-09-21. Evidence comes from
the installed `GameAssembly.dll`, a fresh LibCpp2IL method map, generated interop
metadata and read-only Unity asset inspection. Addresses below identify this
build's evidence; the implementation uses generated properties and Harmony
method lookup, never hardcoded addresses or field offsets.

## Bite, escape and catch

- `FishingMeleeObject.get_FishWaitTime` at `0x181272CE0` first calculates the
  biting-time proficiency bonus, reduces both base range endpoints, then applies
  `fishWaitTimeCustomizationData` to each. The coroutine samples this **fully
  customized** range; scaling the sampled wait preserves these native bonuses.
- Native `<_FishingRoutine>d__75.MoveNext` at `0x18127B1C0` is generated as
  `FishingMeleeObject.__FishingRoutine_d__75.MoveNext()` in the interop assembly.
  State 0 samples `_waitTarget_5__2`, clears elapsed time, then yields state 1.
  State 1 advances fishing only with the sinker in water and bait equipped.
  If the hook container is empty, it advances `_waitTime_5__3`; at the target
  it calls `SinkCatch`, samples `fishEscapeTime` into `_fishEscape_5__6`, and
  invokes `FishItem`. While the hook contains an item it decrements the escape
  timer. Once the timer expires it drops the hook and emits the native failure
  event. Fake sink animation timers are separate and remain unchanged.
- `FishItem` at `0x18126F670` calls the four-argument
  `GetFishableItemInfo(ref FishingResult, WeatherManager, EquipmentItem, Vector3)`
  at `0x180D89F40`. It creates the returned item, adds it to the normal hook
  container, consumes bait and attempts normal bait auto-equip.
- `_OnPullRod` at `0x181271890` first checks use context and the hook container.
  The player branch samples `Random.value`, reads `fishSuccessRate`, optionally
  customizes it, and takes the failure branch only when the roll is greater
  than that threshold (`0x181271B38` through `0x181271B9D`). Its successful branch
  performs discovery/catch handling and calls `DeedsDatabase.SendDeedEvent`.
  `TakeHookItems` later transfers hook items and awards proficiency naturally.
- `ActivateFishing(false)` stops the coroutine and clears `_fishingRoutine`;
  `_OnUnequip` also stops/clears it. This is checked during feature ticks so a
  stopped coroutine is not retained just because the rod is still equipped.

Implementation patches only parameterless `MoveNext()` and `_OnPullRod()`.
No original method with primitive byref arguments is detoured. The generated
byref selection and item methods remain entirely native.

## Exact selection weight and eligibility

Both position-aware selection overloads implement the same eligibility/weight
loop. The player-used four-argument overload at `0x180D89F40`:

1. Resolves `PopulationManager.GetExistingFishes` at the sinker position.
2. Accepts base-list entries, or a non-base entry whose `ItemInfo` matches the
   resolved population fish. Other special fish are skipped.
3. Runs the item's `AvailabilityProcess.CheckAll` against the weather context,
   preserving seasonal/availability restrictions.
4. Calls `CheckBait`, requiring a matching equipped bait and returning that
   bait's additive `chanceModifier`.
5. Calls `CheckBiomeAvailability`, preserving water biome, available fishing
   ground and depletion checks.
6. Uses the effective weight:

   `chance + matchingBaitModifier + (matchesPopulationFish ? chance * bonusChanceForSpecialFish : 0)`

7. Sums eligible weights, samples native `Random.Range(0, total)` and walks
   cumulative weights. Selection and normalization remain native.

The important arithmetic instructions are `0x180D8A24C` through
`0x180D8A2AA`. Increasing `chance` alone does **not** multiply the full weight
because bait contributes an additive amount. Therefore the feature scales both
the special fish's `chance` and every copied bait `chanceModifier`, leaving
`bonusChanceForSpecialFish` unchanged. This gives exactly 2x/4x relative weight
for an eligible special fish, without turning the UI label into a probability
or a guarantee. The normal preset leaves the original configuration untouched.

## Asset verification: special entries are fish

Read-only UnityPy inspection found `FishableItems`, a `FishableItemsConfig` in
`sharedassets0.assets`, object path ID 136782. The serialized configuration was
decoded in declared field order and consumed exactly **936 of 936 bytes**:
nine fish entries, each with four bait entries, four base fish references and
`bonusChanceForSpecialFish = 1`. There are no trash/junk entries in this asset.
Only asset identities and numeric observations are recorded here; game asset
data is not copied into the repository.

| Classification | Asset name | Chance | Bait modifier values |
| --- | --- | ---: | --- |
| Base | Item_Food_FishSeabass | 20 | 3, 3, 3, 5 |
| Special | Item_Food_FishSalmon | 100 | 5, 5, 5, 5 |
| Special | Item_Food_FishCod | 100 | 4, 4, 4, 4 |
| Special | Item_Food_FishMakerel | 100 | 3, 3, 3, 3 |
| Special | Item_Food_FishSturgeon | 100 | 1, 2, 2, 5 |
| Special | Item_Food_FishWolffish | 100 | 1, 1, 1, 5 |
| Base | Item_Food_FishPike | 100 | 1, 1, 1, 5 |
| Base | Item_Food_FishPerch | 100 | 1, 1, 1, 5 |
| Base | Item_Food_FishCatfish | 100 | 1, 1, 1, 5 |

The first six entries have biome mask 2; the final three have mask 4. Item/bait
identities and masks remain unchanged by the assist. `Makerel` is the asset's
actual spelling. Classification uses the native base list, not display names.

## Scope, restoration and bounded work

- The existing HostedFeature session guard runs each callback. Additionally,
  the rod must be equipped, in `MeleeObject` use context, attached to the current
  local player's geometry, and in a master session. Villager/foreign rods fail
  these checks.
- One local coroutine owns sampled timer adjustments. Changing speed rescales
  target and elapsed time together, preserving the completion fraction.
  Easy catch extends a real bite's remaining escape window by four. Returning
  it to normal divides the remaining extended time by four, preserving its
  remaining fraction. Disable restores owned timers on the same live local rod.
  A changed player/scene releases stale ownership without dereferencing its
  old native scene objects beyond Unity's liveness/identity checks.
- Rare selection uses a private ScriptableObject clone and fresh special-fish
  and bait records. Base records and item/availability references are read-only.
  Cloning occurs only in state 1 when the hook is empty, sinker is in water,
  bait exists and the next native tick reaches the bite threshold. Lists are
  bounded to 256 fish/base entries and 128 bait entries per fish. Invalid,
  negative, nonfinite or overflowing weight data fails visibly through the
  feature circuit breaker before assigning a replacement to the rod.
- The original rod config is restored in a Harmony finalizer, including when
  the original native call throws. Private clones are destroyed after the
  native call. Cleanup scopes are retained until release succeeds; Disable
  retries inactive scopes, while active callbacks release in their finalizers.
  Disable reports pending cleanup while any active scope remains, keeping the
  HostedFeature retry lifecycle open even if a session gate closes inside a
  callback and that callback's later finalizer encounters a cleanup failure.
- For easy catches, only the valid local pull callback temporarily receives
  success threshold 1 with its success customization selector bypassed. Both
  values are restored in a finalizer, and unrelated changes are not overwritten.
  Random sampling itself remains native and unpatched.
- The per-step scope is reused for its cast; diagnostics format a status only
  when settings/counters change and log at most 12 bite observations per enable.
  Native cast/bite/reel, bait consumption, normal items/proficiency and deed
  emission remain required. No achievement state or global RNG is patched.

## Verification and remaining gameplay checks

Pure tests exercise the full additive native weighting formula, normalization,
zero and bait-only weights, invalid/overflow data, wait preset transitions and
fraction-preserving restoration of the easy-catch window. Root coordinates the
full test suite, interop gate and plugin build to avoid simultaneous builds.

This research and build validation are **not a live gameplay pass**. Verify a
normal cast at each speed; bait use and item transfer; late but valid easy reels;
normal timing after turning assists off during a cast/bite; cancellation,
unequip, reload and session changes; and fish eligibility/ordinary deed progress.
Rarity changes must be sampled across eligible fishing locations and seasons.
