# Tribe building, harvesting, and gathering work

Native audit: ASKA Steam build **25326768**, Unity **6000.3.12f1**, local
`GameAssembly.dll`, IL2CPP metadata version 39, and matching BepInEx interop.
These findings are from native disassembly and metadata, not empty generated
interop method bodies. Runtime acceptance in the game remains pending.

## Controls and ownership

- `TribeBuildSpeedFeature(ITribeContext)` exposes `tribe.buildspeed` and a 1-5
  multiplier, with normal (1x) the default.
- `TribeHarvestSpeedFeature(ITribeContext)` exposes `tribe.harvest` and an
  independent 1-5 multiplier, also defaulting to normal.
- All mutations run through `Hosted.TryExecute` and qualify the current session's
  `IInteractionAgent` with `ITribeContext.IsCurrentVillager`. This retains the
  current world, local team, live owned population member, settlement and native
  authority checks. Guests, dead villagers, other teams and players fail this
  ownership test. Gathering and building additionally require the native master
  session on their interaction.
- No periodic population enumeration, global time changes, global animator
  changes, direct skill edits, resource duplication or forced completion occurs.
  Current-villager membership checks use the existing context's live population
  membership lookup; gather callbacks therefore still have per-frame ownership
  validation cost for each active gatherer.

## Building

`VillagerBuildInteractionConfig.VillagerBuildSession._OnAnimatorEvent(string)`
starts at `0x180CEBDE0`. Its matching damage event obtains `BuildConfig.moveset`,
computes the weighted attribute sum, and then computes:

`work = (attributes * moveset.damageMultiplier + moveset.baseUnarmedDamage) * _buildTime`

The multiplication/addition are at `0x180CEBFAB` through `0x180CEBFB5`; the original
`BuildInteraction.RequestAddBuildVolume` call is at `0x180CEC02F`.
The event also handles equipment wear, work injury, proficiency and clears
`_buildTime`; those native paths and inputs remain intact.

The feature opens an event scope only for a running owned villager's matching
event, after target matching and action start. Within that synchronous event,
`BuildConfig` returns a private configuration/moveset clone with **both** work
coefficients multiplied. Original asset names are retained for localization.
Shared assets and the session's stored configuration are never changed. A
finalizer releases both temporary Unity objects and restores any parent scope,
including exception paths. Cleanup failure is logged and disables the feature.

No patch is installed on `RequestAddBuildVolume(ref float)` or another original
primitive-by-reference work method. The previously observed IL2CPP detour freeze
on that approach is not reintroduced. Materials, project validity and native
completion remain checked by the original submission.

## Harvesting (chopping/mining)

`VillagerHarvestInteractionSession._OnAnimEvent`, at `0x180D201C0`, invokes
`_DealSimulatedDamage` for the configured event after positive `_harvestTime`.
`Update`, at `0x180D1F890`, also invokes the same method for the native fallback
interval when no damage event is configured. The feature scopes
`_DealSimulatedDamage` (`0x180D1FBA0`), so both native paths are supported.

That method creates a fresh `DamageData`, whose constructor initializes only its
damage multiplier to 1 (`0x18127C750`). It resolves the acting transform's
`GetComponent<IDamageDealer>()`, assigns the dealer, resolves required equipment,
reads its damage property **1002**, then calls `HarvestInteraction.TakeDamage` at
`0x180D1FFD7`. The generic method global at `0x185CA3250` resolves to
`UnityEngine.Component.GetComponent<SSSGame.Combat.IDamageDealer>` in LibCpp2IL.
The damage dealer is a separate component (such as `MeleeManager`), **not** the
villager object. The ownership guard repeats this exact component lookup and
matches the resulting native pointer. `IDamageDealer` natively inherits
`IPropertyContainer`; this is the property source used for weighted attributes.

`TakeDamage` computes this work at `0x180DDD848` through `0x180DDD861`:

`nativeWork = (1 + attributeBonus * moveset.damageMultiplier) * weaponDamage * data.damageMultiplier + data.baseDamage`

The weighted attribute loop matches `InteractionMoveset.GetAttributeBonus`.
The feature reads this native bonus through the actual dealer's property
container. Required-tool sessions re-read property 1002 and require it to equal
the fresh event's base damage before changing anything. No-tool sessions use
zero for weapon damage. It substitutes only:

`data.baseDamage += (preset - 1) * nativeWork`

This makes total work equal to `preset * nativeWork` while preserving
`data.damageMultiplier`. That distinction matters: native proficiency uses the
latter at `0x180DDD9E0`, and native tool wear uses it at `0x180DDDC27`. Scaling the
damage multiplier would unintentionally multiply both award and wear inputs.
Native proficiency is still called normally, so the separate skill-gain feature
can independently scale that native award.

The hook consumes its scope once, requires the exact target, running owner,
current moveset, zero fresh result/flags, and expected dealer/tool identity.
Nested harvest scopes mask and restore parent scopes. The `TakeDamage` finalizer
restores only its owned base-damage write independently of the execution gate;
the native result remains the actual increased work. Exceptions propagate, and
cleanup errors are logged and disable the feature. Native equipment category,
tool tier, damage validity, network notification, resource health, loot and
completion paths execute normally. Combat damage and player harvest callbacks
have no qualifying villager simulated-work scope and remain unchanged.

## Gathering

`VillagerGatherInteractionSession.Update`, at `0x180D1DF60`, selects
`GatherInteraction.Moveset`, falling back to `GatherConfig.defaultMoveset`.
It gets the agent's attributes, calls `GetAttributeBonus`, then subtracts:

`deltaTime * (moveset.baseUnarmedDamage + attributeBonus * moveset.damageMultiplier)`

from its remaining `_duration`. When duration reaches zero it resets to the
native `gatherVolumePerCharge`, processes a native charge, and applies native
quantity, capacity, bonus-loot, proficiency and completion rules.

The instruction order is significant: the call to `GetAttributeBonus` is at
`0x180D1E0F4`, and the read of `_duration` into `xmm9` is at `0x180D1E0F9`, **after
the call returns**. The subtraction/store are at `0x180D1E127`/`0x180D1E130`.
Thus an exact-session, exact-moveset, exact-attribute-container postfix can
subtract only `(preset - 1)` additional ticks of actual native work before the
native code reads `_duration`. It does not change the returned bonus or shared
attributes. The scope is consumed once and removed in the Update finalizer.

Use/consume sessions (`IsUse` or `ForceUse`) are excluded. This avoids changing
food/drink consumption under a harvesting control. Gathering processes at most
one native charge per Update; overshoot is discarded by the native reset, so
very short gathering cycles are frame-limited rather than guaranteed to produce
exactly five times the wall-clock item rate. Walking, carrying, inventory
capacity and AI scheduling likewise remain native.

## Reset, limits, and validation

Normal (1x), reset, configuration reload and feature disable stop subsequent
boosts. Synchronous scopes restore parent context and leave no saved animator,
stat or threshold override requiring later scene cleanup. Work already earned
stays earned; restoring a preset does not roll back construction or resource
work. Build clone cleanup also runs from callback finalizers when a gate closes.

Only these three verified native session paths are supported. This feature does
not claim to accelerate crafting stations, hauling, fishing, farming timers,
combat, global time or any separate/offscreen simulation path that bypasses
these methods. Unexpected state fails closed; non-finite math and overflow throw
before the native write and are handled by the hosted feature's fault gate.

`TribeWorkSpeedMathTests` checks presets 1-5, full building coefficients, complete
tool and unarmed harvest formulas with fractional native multipliers, additive
gather work and native completion boundaries, plus invalid/overflow inputs.
The helper performs no array allocations per gathering tick. Status is published
at most once per second or when the selected preset changes; building logs are
bounded to the first 12 qualifying event reports.

Root integration verified the final dealer-identity correction in a successful
local-interop build with zero warnings/errors and 245 passing Core tests.
Independent source/native review found no remaining actionable defects. In-game checks
still required: 1x/2x/5x tool and unarmed harvest, gathering without consuming,
multi-villager construction with normal material requirements, cancellation,
tool breakage, target completion, reset mid-work, save reload and return-to-menu.
No game launch, install, commit or concurrent project build was performed by the
work-speed implementation agent.
