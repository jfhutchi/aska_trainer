# Temperature protection and aging native audit - 2026-09-14

## Evidence and limits

Read-only inspection of the user's local ASKA Steam build 25186770, Unity
6000.3.12f1. Mono.Cecil verified generated interop signatures. The bundled
LibCpp2IL resolved native methods and field layouts from GameAssembly.dll and
global-metadata.dat; Capstone was used to trace only relevant methods. No live
game, save, installation, or binary was modified. Native address mappings and raw
disassembly remain temporary local artifacts, outside the repository.

The native results justify protection from *further* temperature deterioration.
They do not justify resetting warmth to an invented comfortable level, clearing
status effects, or declaring a currently freezing character recovered.

## Warmth

`CharacterSurvival.GetWarmthPerSecond(float)` computes the felt temperature and
warmth change rate, including resistance and environmental effects. Its original
body updates `_feltTemperatureAttr` and `_warmthChangeRateAttr`, then returns the
proposed rate. The `_warmthModifier` callback
`__InitalizeAttributes_b__75_4(float currentValue, float min, float max, float deltaTime)`
calls this function and multiplies its result by `deltaTime`.

`VariableAttribute.Tick(ref float)` sums modifier deltas and adds them to the
current value before invoking native `SetValue`. Therefore a negative returned
warmth rate decreases warmth, and a positive rate allows recovery. The protection
postfix preserves original computation/side effects and replaces only a negative
returned rate with zero.

`CharacterSurvival._OnWarmthChanged` considers warmth at or below zero freezing,
subject to native debug, suspension, death, and golem conditions. It adds/removes
the native freezing status effect and publishes suffering changes. The trainer
does not intercept this method. A character already at zero warmth can retain
the freezing status and its existing penalties until it warms up normally.

## Frost

The native initialization binds `_frostChangeModifier` to
`__InitalizeAttributes_b__75_3(float currentValue, float min, float max, float deltaTime)`
and registers that modifier on `_frostVAttr`. This callback reads `_blizzardAttr`:

- A positive blizzard value produces a positive frost accumulation delta.
- Without that exposure it produces a negative thaw delta from the native warmth
  change rate, bounded so cooling cannot become thawing.

The protection postfix replaces only a positive frost delta with zero. Negative
thaw remains native. It does not clear existing frost, alter wetness, fake weather,
edit equipment resistance, or suspend unrelated survival attributes.

Relevant native RVAs, recorded only for research reproducibility:

| Method | RVA |
| --- | --- |
| CharacterSurvival.GetWarmthPerSecond | 0xB7B3E0 |
| CharacterSurvival frost modifier callback b__75_3 | 0xB7BFE0 |
| CharacterSurvival warmth modifier callback b__75_4 | 0xB7C0C0 |
| CharacterSurvival._OnWarmthChanged | 0xB7D880 |
| VariableAttribute.Tick | 0x35D3C60 |

## Scope and lifecycle

Player protection resolves `IPlayerContext` and compares the callback's survival
component to the current local player's survival. Tribe protection resolves the
survival owner, verifies `ITribeContext.IsCurrentVillager`, and compares the owner's
current survival component. Every attempted change runs through the feature's
refreshed single-player guard and circuit breaker. Recovery or zero-change results
are preserved without unnecessary ownership discovery.

The two toggles install independently owned Harmony postfixes. Disabling either
removes only its own patches. No native value or modifier is retained for later
restoration, and no per-villager wrapper cache is added. Both toggles explicitly
state that existing cold/frost requires normal recovery.

The compiled callback name is version-specific. Compatibility fails if its exact
four-float signature is absent. A future game update can also change native
semantics without changing signatures, requiring renewed inspection and smoke
testing; reflection alone cannot establish semantic compatibility.

## Freeze Aging remains unavailable

The suggested `_lifetimeVariableAttribute` / `_lifetimeModifier` pair is not proof
of normal villager aging. Native `VillagerSurvival._InitalizeAttributes` branches
on `Villager.IsGolem()`, and creates/registers this lifetime modifier only on the
golem branch. Its callback b__121_1 consumes an accumulated time value, clears it,
and returns negative elapsed time normalized by a native day duration when not
suspended. `_OnLifetimeReachedMin` sets `DiedOfOldAge` and kills the character.

This is a verified golem lifetime/expiry path. A general Freeze Aging feature must
not silently target it as though it ages every ordinary villager. Searches for
ordinary villager aging, birthdays, age setters, and other lifetime callbacks did
not establish a separate supported hook. Freeze Aging therefore remains
Incompatible with a specific explanation. Pet aging and crop age APIs are outside
the requested tribe scope and are untouched.

## Acceptance still required

Pure rate tests cover warming through repeated cold exposures, thawing through
repeated blizzard exposures, zero rate without invented recovery, and rejection
of non-finite rates. Runtime IL2CPP dispatch and behavior need loaded-save checks:

1. Starting warm, enable player protection and expose the player to cold/blizzard:
   warmth should not decrease through the protected callback and frost should not
   accumulate. Other needs and weather must continue normally.
2. Enable while already freezing or frosted. Existing penalties may remain; move
   to warmth and confirm normal warming/thawing still works.
3. Repeat for owned living villagers. Guests, foreign teams, and unrelated
   characters must be unaffected. Player and tribe toggles must work independently.
4. Disable, Reset All, quit/reload, and lose the single-player gate. Ordinary
   temperature changes must resume with no stale wrapper or restored-value writes.

No live gameplay success or full immunity to pre-existing freezing damage is
claimed by these changes.
