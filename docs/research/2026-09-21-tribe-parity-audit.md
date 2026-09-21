# Player and tribe skill gain / parity audit

Read-only native and source audit: 2026-09-21, ASKA Steam build 25326768.
No game files, saves, installed plugins, or native dependencies were changed.
The conclusions below establish hook contracts and source scope, not gameplay acceptance.

## Skill award contract

The installed generated signature is:

```csharp
void InteractionMoveset.AwardProfficiencyPoints(
    IInteractionAgent agent, float amount, SandSailorStudio.Attributes.Attribute cap);
```

Native entry address is `0x180DE1630` (RVA `0xDE1630`). The method copies
the second explicit parameter from `xmm2` at `0x180DE165B`; it is a scalar
passed by value, not a pointer to a float. A Harmony prefix `ref float __1`
changes that managed invocation's argument. It does not make the native
method a primitive-by-reference detour.

The native body retains these operations after an incoming-amount multiplier:

- Zero-amount, missing expertise, nonpositive expertise coefficient, missing
  leveling path, and existing maximum-level checks.
- Multiplication by the moveset's `expertiseGainMultiplier` at `0x180DE18E3`
  and the eligible expertise attribute multiplier at `0x180DE1914`.
- Addition to the existing experience remainder and evaluation of the native
  level cost curve at `0x180DE195F`.
- Supplied-cap or agent workstation-cap qualification at
  `0x180DE19D7..0x180DE1A16`.
- Native level increments, configured maximum-level clamp, event dispatch,
  player visual effects and player/villager level-up reporting.
- Recalculation of the next level cost and storage of the experience remainder.

The workstation cap check precedes the level-up loop. It is not repeated
inside that loop. Say that the feature preserves *native cap checks*; do not
claim a large award can never cross a workstation cap in one invocation.
Direct attribute assignment, generated characters and loading saved values
are not earned-experience awards and are outside this hook.

## Installed bridge inspection

Read-only Mono.Cecil inspection of the installed
`Il2CppInterop.HarmonySupport.dll` and `0Harmony.dll` confirms the distinction
from the withdrawn original-by-reference build and durability hooks:

1. `Il2CppDetourMethodPatcher.GenerateNativeToManagedTrampoline` derives
   native argument types from `Original.GetParameters()` at IL
   `0070..00A4`, not from the prefix signature.
2. `EmitConvertArgumentToManaged` emits `ldarg.s` and returns immediately
   for the original `System.Single` value type at IL `0092..00AB`.
   Its separate original-by-reference conversion (`ldind.i` and a writeback
   local) is not taken for this award argument.
3. `HarmonyManipulator.EmitCallParameter` handles an original value parameter
   paired with a prefix reference parameter of the same value type by
   emitting `ldarga`: IL `0939..0949` branches to `09D6..09E3`.

This supports the selected signature at the installed bridge level. It does
not establish successful live detour invocation. Neither dependency was
patched or replaced.

## Verified callers

A scan of relative native `call`/`jmp` instructions, validated by decoding
from the mapped enclosing method start, found 39 direct callsites to the
award method. Method boundaries and names came from current-build generated
metadata; the game binary supplied the instructions.

| Activity | Verified callers / callsite addresses |
| --- | --- |
| Cooking | `CrockpotInteraction.StartCooking`, `0x180BAFA71` |
| Farming, player | `FarmCropInteractionSession.<Update>b__41_0`, `0x180C7A8FC`; `b__41_1`, `0x180C7AE66`; `Update`, `0x180C7B915`; `_OnAnimatorEvent`, `0x180C7CE1C` |
| Farming, villager | `VillagerFarmCropInteractionSession.<Update>b__18_0`, `0x180D17423`; `b__18_1`, `0x180D176ED`; `b__18_2`, `0x180D17BD0`; `_OnAnimatorEvent`, `0x180D191BD` |
| Fishing, player | `Combat.FishingMeleeObject.TakeHookItems`, `0x1812702DB` |
| Fishing, villager | `VillagerFishingInteractionSession.Update`, `0x180D1CB78` |
| Melee combat | `Combat.MeleeManager.TriggerAoE`, `0x181282006`; `_HitCheck`, `0x1812836BF` |
| Ranged combat | `Combat.RangedManager.OnProjectileDamage`, `0x181293AD2` |
| Harvesting / mining | `HarvestInteraction.TakeDamage`, `0x180DDD9F3`; `CaveWallInteraction.TakeDamage`, `0x180D4A8E2` |
| Gathering | Player session `Update`, `0x180C9D6F5`; villager session `Update`, `0x180D1E67B` |
| Construction | Player session `_OnAnimatorEvent`, `0x180C845D3`; villager session `_OnAnimatorEvent`, `0x180CEC1C2` |
| Crafting | Player session `Update`, `0x180C89D49`; villager session `Update`, `0x180CEF0F9` |
| Anvil | `AnvilInteraction.TakeDamage`, `0x180B77370`; player `_DealSimulatedDamage`, `0x180C7055E`; villager `_DealSimulatedDamage`, `0x180CE8B94` |
| Bellows | Player `Update`, `0x180C813E6`; villager `Update`, `0x180CEA2E6` |
| Healing | Player `_OnAnimatorEvent`, `0x180C92410`; villager `_OnAnimatorEvent`, `0x180D21719` |
| Fire | Player `_OnVolumeProgressReset`, `0x180C981F3`; villager `_OnVolumeProgressReset`, `0x180D1B284` |
| Painting | Player `_DoPaintAction`, `0x180CAD2E5`; villager `_DoPaintAction`, `0x180D25FDB` |
| Repairs | Player `_OnAnimatorEvent`, `0x180CBB8D3`; villager `_OnAnimatorEvent`, `0x180D3FC24` |
| Prayer | Player `_OnPraystationEvent`, `0x180CC05A6`; villager `_OnPraystationEvent`, `0x180D2B75B` |
| Study | Player `_OnKnowledgeFull`, `0x180CE435B`; villager `_OnKnowledgeFull`, `0x180D458A8` |

This establishes broad shared earned-experience coverage, including combat,
farming, cooking and both fishing flows. It is not a proof that every
possible progression write goes through this method. No separate verified
earned-skill path requiring another hook was identified by this audit.

The owner gate must remain independent for the two features: current local
`PlayerInteractionAgent.GetCharacter()` for player awards, and a successful
`TryCast<Villager>()` plus `ITribeContext.IsCurrentVillager` for tribe awards.
The latter requires live registered population membership, local team,
current settlement, state authority, no guest station, and a living villager.
Both features use the existing single-player feature gate. Merely being an
`IInteractionAgent` is insufficient ownership evidence.

## Player-option parity

The user's broad parity request cannot be represented as every player toggle
working unchanged for villagers. Existing source and current native metadata
support this more precise breakdown.

| Player option | Tribe equivalent or concrete remaining work |
| --- | --- |
| God mode | Existing `tribe.god` applies to current owned villagers. |
| Hunger / thirst | Existing `tribe.food` and `tribe.water` maintain verified villager needs. |
| Temperature protection | Existing `tribe.temperature` has its own native villager path. Warmth editing remains read-only. |
| Stamina | Tribe energy and rest controls already exist, but they are not proof of a combat-stamina equivalent. The player feature hooks `Character` / `CharacterMovement`; a separate villager stamina contract is not established here. |
| Earned skill gain | Shared award contract verified above; independent player and tribe controls are the current implementation scope. |
| Harvest and build speed | Separate tribe work-session features are implemented. [Their native evidence](2026-09-21-tribe-work-speed.md) defines coverage; player session hooks cannot simply be reused. |
| Movement speed | Separate tribe ground-navigation feature implemented after the additional contract trace below. Ordinary authoritative ground movement only; native acceleration, navigation and task selection remain. Runtime arrival/carrying behavior still needs acceptance. |
| Fishing assists | Current player feature explicitly requires the locally equipped rod and `UseContext.MeleeObject`. Villagers have a separate `VillagerFishingInteractionSession`, `BitingTime`, success configuration, bait and catch handling. Shared fishing experience is covered; bite/rare/easy-catch assists are not implemented for villagers. |
| Free Building | Existing structure supply waiver targets the current settlement's authoritative construction part, not a player actor. It already supplies shared construction eligibility for normal player/villager work. Native AI scheduling and work are still required. |
| Free Repairs | Existing supply waiver targets the current settlement's authoritative repair part. It already grants shared repair eligibility; normal worker selection and repair work remain native. |
| Ignore Crafting Materials | Current implementation explicitly requires the local player's interaction agent and matching owned inventory. Tribe extension needs the villager material reservation / product creation transaction established first. No tribe free-crafting control is implemented. |
| Retain Items on Use | Existing transaction and item ownership gates are local-player-only. Villager consumption and needs effects require their own verified transaction. Food/water maintenance already addresses needs but does not establish item retention. |
| Infinite Durability / No Spoilage | Both player features are currently withdrawn incompatible stubs after native trampoline failures. Tribe support is also unavailable; old research describing implementation is superseded by the 0.1.6 retest record. |
| Expanded terrain leveling | Current player feature is withdrawn after a preview crash; no tribe counterpart is implemented. |
| Inventory spawn / give items | Existing inventory service resolves the local player's inventory. There is no verified tribe-wide give-item transaction in this audit. |
| World time / game speed / fuel / mushroom regrowth | World or structure systems are shared by the simulation. Separate tribe copies would misrepresent their scope. Existing feature-specific limits still apply. |

Freeze Aging remains unavailable because an ordinary-villager lifetime hook
has not been established; golem-only evidence is insufficient. Recruitment,
needs restoration, happiness and the existing selected-villager editor are
tribe-specific controls already present.

## Additional ground-navigation contract and implementation

`CreatureController._GetMovementSpeed(AIMovementSpeed)` returns a scalar float
at RVA `0x80E790`. The public `MovementSpeed` property instead returns an enum
and is not a numeric speed hook. Native `Update` calls the scalar method at
`0x18080C86C`, smooths the returned target against current agent speed using
native acceleration, then calls `NavMeshAgent.set_speed` at `0x18080C95F`.
The scalar method already includes native braking and movement multipliers.

Two other calls serve link traversal: `_StartOffMeshLink` at `0x1808142AA`
and `_TraverseLink` at `0x180814FFD`. Link setup writes
`_linkTraversalState.valid = true` at `0x180814256`, before querying speed.
Traversal itself requires that same flag at `0x180814F28`. These facts permit
the following narrow implementation without patching traversal methods:

- Scope a scalar-return postfix inside the same controller's `Update`, using
  a thread-local scope restored by a finalizer even on native failure.
- Recheck the link-state valid flag and `NavMeshAgent.isOnOffMeshLink` at the
  speed query, excluding setup and traversal even if they begin during Update.
- Require an initialized, ready, owned, grounded, moving controller; exclude
  swimming, vehicles, ladders, stopped/locked movement and action root motion.
- Resolve `NavAgentControllable.TryCast<Villager>()`, require exact pointer
  identity with `Villager.GetControlAI()`, then current tribe membership.
  Native getters return their actual stored references at `0x180816F10`
  and `0x180DAB5C0`, respectively.
- Scale only the fresh scalar result with existing tested
  `MovementDelta.ScaleNativeSpeed`; retain zero and reject nonfinite/overflow.

`TribeMovementSpeedFeature` implements this contract. It changes no shared
parameters, controller fields, destination, agent speed field, or save state.
After disable, the next native update supplies the original target and the
game's acceleration smooths the current speed normally. This control changes
target speed; it does not promise an instantaneous matching distance ratio.
The existing native-speed arithmetic tests are reused. No live navigation
test or compilation result is claimed by this independent audit.

## Concrete fishing extension candidates

Villager fishing is feasible additional work, with distinct state ownership:

- `get_BitingTime` at RVA `0xD1D4F0` computes a Vector2 range after native skill
  reduction and customization. Start/Update sample the range into
  `_volumeTarget`; Update increments `_volumeProgress` with delta time.
  Owned-session timer rescaling or verified result scaling can accelerate
  bites while leaving the native catch operation intact.
- Update reads `FishingConfig.SuccessRate` and its customization selector at
  `0x180D1C751..0x180D1C7C7`, then passes the result to `FishItem`. A private
  config returned only for the current owned Update could supply success 1
  and a null selector. `FishItem` has original byref outputs and should remain
  unpatched, as in the other repaired features.
- `FishingInteraction.FishableItems` supplies the selection configuration
  read by `GetFishableItemInfo` at `0x180DD8408`. A temporary private weighted
  clone is a candidate, but exact-agent scoping, shared interactions with
  `allowMultipleAgents`, reentrancy and cleanup need verification first.

These are candidate contracts, not implemented tribe fishing assists.

## Focused gameplay acceptance

After the authorized installation/restart window, compare 1x and a higher
skill multiplier on the same repeatable action below a native cap. Check one
player and one current owned villager independently, then enable both to
confirm no doubled application. Include combat, farm/cook work and fishing
representatives because they reach the shared hook through different callers.
Check normal level-up behavior and return to 1x. Reject gameplay-success
claims based only on metadata, arithmetic tests or compilation.

For movement, compare normal ground travel and return to 1x, then verify
arrival at workstations, carrying, combat, swimming, ladders and links remain
functional. Confirm animals, enemies and guests receive no boost.

For additional parity work, investigate villager fishing assists,
consumption and crafting independently. Do not enable placeholders or apply
player ownership assumptions to NPCs to make the options appear complete.
