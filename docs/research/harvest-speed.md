# Harvest speed native evidence

Target: local ASKA Steam build 25186770, Unity 6000.3.12f1, IL2CPP metadata 39.
Read-only analysis used installed GameAssembly.dll and global-metadata.dat with
the BepInEx-bundled LibCpp2IL parser and Capstone disassembly. Addresses below
are evidence locations in this build, not runtime patch offsets. Runtime hooks
use named generated interop methods through Harmony.

## Harvesting

`PlayerHarvestInteractionConfig.PlayerHarvestInteractionSession.Update`
(0x180CA1180) first requires `SessionState == RUNNING` (2) and nonnegative
`_harvestTime`. `Start` initializes `_harvestTime` to -1 until target matching
finishes; `_OnMatchTarget` changes it to zero. Thus starting an interaction alone
does not authorize faster animation.

Update starts `CharacterGeometry.RunAction(ref moveset.attackActionName,
moveset.actionId)` and records `_startedAction`. It increments `_harvestTime`
by `Time.deltaTime`. When `animatorDamageEvent` is empty, it compares this timer
with `fallbackTriggerInterval`, calls `_DealSimulatedDamage`, and resets it.
`_OnAnimEvent` (0x180CA2370) otherwise compares the incoming event with
`animatorDamageEvent`, calls the same native damage method once, and resets
the timer. Merely changing the timer does not accelerate event-driven swings.

The feature changes only the local player's animator while this exact session
is RUNNING, its target is matched, and its positive action identity checks pass. It retains
the native animator speed, preserves later native changes, and restores only
if animator identity and the last assigned speed still match. `End` and
`ResetSession` restore before native cleanup; disable and 1x also restore.
No global time scale is changed. Eventless movesets receive extra elapsed time
in their own `_harvestTime`, while native damage and completion code still run.

The original 0.1.3 implementation excluded movesets with
`allowMeleeAttackWhileHarvesting`. The user reported ineffective tool harvesting,
and read-only inspection of shipped assets proves this blanket exclusion blocks
ordinary chopping and mining. `Start` reads that flag at 0x180CA0779 and sets
`MeleeManager.PreventHarvestDamage` to its inverse; it does not mean the harvest
action is absent.

`sharedassets0.assets` contains these `InteractionMoveset` assets (script reference
1883, resolved to the `SSSGame.InteractionMoveset` MonoScript in
`globalgamemanagers.assets`):

| Asset | Path ID | Action / integer | Damage event | Melee allowed |
| --- | --- | --- | --- | --- |
| Moveset_HarvestGenericAxe | 135059 | ChopGround / 1 | ResourceChop | true |
| Moveset_HarvestGenericPickaxe | 135060 | ChopGround / 3 | ResourceChop | true |
| Moveset_HarvestStump | 135065 | ChopGround / 1 | ResourceChop | true |
| Moveset_HarvestTreeAxe | 135066 | Chop / 1 | ResourceChop | true |

UnityPy read the asset headers and raw serialized data. The remaining field order
was checked against the generated `InteractionMoveset` type: sprite reference,
submoveset integer, action name, action ID, equip-handle reference, display
duration, event name, fallback interval, then the melee-allowed boolean. This is
asset evidence, not a guess based on asset names. No serialized asset was changed.

The corrected guard requires the session's `_startedAction` to equal both the
moveset action name and the live geometry's `_lastStartedAction`, with
`Animator.GetInteger(actionName)` equal to the nonzero configured action ID. Native
`CharacterGeometry.RunAction(ref string, int)` at 0x180B4ED80 writes the last action
and sets that animator integer. `StopAction(string)` at 0x180B4FD20 clears the
integer and clears the matching last action. The generated `GetInteger(string)`
wrapper was inspected and invokes the native method through
`il2cpp_runtime_invoke`; it is not a nonfunctional managed wrapper stub.

Combat mode is excluded independently. A local
`PlayerInteractionAgent.SetCombatMode(true)` prefix restores the owned animation
speed before combat begins. Timer fallback uses the same positive harvest-action
guard and still applies only when no native damage animation event is configured.
Character ownership is checked through `session.PlayerAgent.GetCharacter()` against
the live local player. Villager harvest sessions are different types and are not
patched. Shared moveset settings remain unchanged.

## Gathering

`PlayerGatherInteractionConfig.PlayerGatherInteractionSession._GetGatherVolume`
(0x180C9CBB0) returns the interaction's work volume, after applying the game's
`GatherConfig.gatherSpeedCustomization.GetModifiedValue` if configured.
`BeginAction`, the repeated-action path in `OnEnd`, and the cycle-reset path in
`Update` call this method to set `_volumeTarget`.
The only other direct call sites in this binary are `CancelAction` and
`OnGatherCanceled`; both also store the result immediately in `_volumeTarget`
before routing through the native interaction abort/End lifecycle. No display-only
call site was found, so the getter's ownership record corresponds to a native
threshold assignment.

`Update` (0x180C9BA80, work increment at 0x180C9BCA9) adds
`deltaTime * (moveset.baseUnarmedDamage + moveset.damageMultiplier *
moveset.GetAttributeBonus(attributes))` to `_volumeProgress`, then performs
the original target check, item acquisition, inventory-capacity handling,
proficiency award, and completion logic. The patch divides the native work
threshold by the chosen multiplier for a RUNNING local player's gather
session. It does not modify inventory grants, item yield, shared moveset
assets, or the game's normal gather update method.

Preset changes take effect at the next gather cycle. On disable or 1x, a
still-owned threshold is restored and progress is rescaled to preserve the
current completion percentage. End/reset cleanup is scoped to that session.
Unload/replacement discards ownership instead of touching a stale native
animator or a previous player's gathering session.

## Validation and limits

Core tests cover 1x/2x/3x/4x work thresholds, nonunit animator baselines,
noncompounding preset changes, external speed changes, owner mismatch,
paused animators, and completion-preserving gather cleanup. Compilation checks
validate the named interop APIs. Native behavior above is disassembly evidence;
it is not a claim of successful in-game speed measurement.

Manual acceptance remains: time equal-health chopping and mining at each
preset, gather the same resource at each preset, release/cancel during an
action, change preset, disable, reset, exit/reload, and verify normal walking,
combat and villagers afterward. Confirm native tool durability, stamina,
capacity and loot rules remain in effect. Fishing, farming, crafting, and
other interaction types are not covered by these hooks.

The correction adds seven Core cases for tree/pickaxe action identity, stopped
animation, conflicting action, missing action and zero action IDs. The parent
integration owns compilation and test execution; this worker did not build or
install the correction.

For the next retest, each enable records at most twelve five-second diagnostic
windows under `HutchASKA.Harvest` in the BepInEx log. These count native session
updates, local matches, animation writes, eventless timer advances and rejection
reasons. They include the observed moveset/melee flag, session/geometry action,
expected/observed animator integer, and observed/applied animator speed. A short
sample is also shown below the slider. Thus missing hook dispatch, wrong action
ownership, or subsequent native speed changes can be distinguished if the visible
effect is still absent. Extra observations stop after the twelfth report;
disable/re-enable starts a new sampling window.
