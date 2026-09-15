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
is RUNNING, its target is matched, and `_startedAction` is nonempty. It retains
the native animator speed, preserves later native changes, and restores only
if animator identity and the last assigned speed still match. `End` and
`ResetSession` restore before native cleanup; disable and 1x also restore.
No global time scale is changed. Eventless movesets receive extra elapsed time
in their own `_harvestTime`, while native damage and completion code still run.

Movesets with `allowMeleeAttackWhileHarvesting` are excluded. `Start` reads that
flag at 0x180CA0779 to set `MeleeManager.PreventHarvestDamage`; it is unsafe to
treat such a session as exclusively harvest animation. Character ownership is
checked through `session.PlayerAgent.GetCharacter()` against the live local
player. Villager harvest sessions are different types and are not patched.

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
