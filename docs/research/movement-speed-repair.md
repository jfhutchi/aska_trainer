# Movement speed repair - 2026-09-14

## Report and investigation

The user reported that enabling Movement Speed and increasing its slider in 0.1.2
did not change travel speed (approximately 115 FPS). No loaded save or live game
memory was accessed during this investigation.

The old implementation added a MULTIPLY modifier to `_moveMultiAttr`. Local native
inspection establishes why this is not a direct travel-speed multiplier:

- `Attribute.AddModifier` marks `_dirtyValue` and invokes change notifications.
  Explicitly dirtying the attribute again is not the missing operation.
- `Attribute._GetComputedValue` applies MULTIPLY with floating-point multiplication.
  The enum operation was not misidentified.
- `CharacterMovement._OnMovementMultiplierChanged` reads `_moveMultiAttr` and maps
  it through `MovementStats.movementAttributeToWalkRatio` to `_walkRatio`.
  The native expression is `range.x + (range.y - range.x) * attributeValue`.
- `MoveCommand` applies `_walkRatio` to the movement input vector used by animation.
  Thus the attribute is an animation/input ratio, not a physical-distance multiplier.
  Input/animation saturation or a zero attribute baseline can make a MULTIPLY
  modifier ineffective. Which of these applied to the user's loaded character was
  not observed; the proven defect is the use of this indirect control as a 1x-5x
  displacement multiplier.
- `OnAnimatorMove` adds the animator's root-motion delta, multiplied by the native
  `animationRootMotionRatio`, to `_totalDeltaRootMotion` when time is advancing.
- `_ApplyRootMotion` processes that accumulated delta through native obstacle
  checks, ground penetration correction, and movement application, then clears the
  consumed accumulator. Multiplying the entire accumulator on each animation
  callback would compound earlier samples before consumption.

## Evidence provenance

Inspected the user's local ASKA Steam build 25186770, Unity 6000.3.12f1. Mono.Cecil
read generated interop signatures and the bundled LibCpp2IL read the local
`GameAssembly.dll` and `global-metadata.dat` to resolve native method addresses.
Capstone decoded the targeted methods. No offsets are used in the plugin.

Relevant native RVAs in this build (research identifiers only):

| Method | RVA |
| --- | --- |
| Attribute.AddModifier | 0x35C8100 |
| Attribute._GetComputedValue | 0x35C8B00 |
| CharacterMovement._OnMovementMultiplierChanged | 0x1244E80 |
| CharacterMovement.OnAnimatorMove | 0x123EF90 |
| CharacterMovement._ApplyRootMotion | 0x1243EC0 |

The parser/disassembler and address index remain temporary local research
artifacts. Proprietary assemblies, binary excerpts, and full disassembly are not
included in the repository or release.

## The 0.1.3 attempt and confirmed missing path

The user subsequently tested 0.1.3 and reported no change in movement speed. The
plugin loaded without errors. That runtime result invalidates the assumption that
the root-motion path alone covers ordinary locomotion. A successful patch install
is not proof that its native callback produces nonzero movement.

Further native tracing of `CharacterMovement.MoveCommand(Vector2, Vector3,
Vector3)` (RVA `0x123B6B0`) found the missing complementary path:

- At RVA `0x123D768`, the native method reads `MovementStats.speed` and multiplies
  its camera-relative, smoothed movement input by that speed.
- At `0x123D771` through `0x123D7C0`, it multiplies that result by
  `Clamp01(1 - animationRootMotionRatio)` and stores `_finalDirection`.
- It retains the native ground projection, backward movement penalty and moving
  platform contribution. At `0x123DF39`, the ordinary locomotion branch assigns
  the resulting horizontal components to the rigidbody velocity while preserving
  its current vertical velocity.
- `OnAnimatorMove` instead multiplies animation displacement by
  `animationRootMotionRatio`. At a ratio of zero, scaling that root-motion delta
  alone always scales zero, while the separate velocity path continues unchanged.

This proves the previous implementation omitted a native movement path. The
specific root-motion ratio and callback/guard outcomes on the user's character
were not logged in 0.1.3; bounded diagnostics now expose those values instead of
assuming which one applied.

## Corrected implementation

An eligible local `MoveCommand` now temporarily receives a privately instantiated
`MovementStats` copy with `speed = current native speed * requested multiplier`.
The native command still handles input smoothing, animation, sprint effects,
ground projection, moving platforms, collision and body velocity. The trainer
does not directly multiply total body velocity, which would also multiply platform
or external motion.

The copy is cached, not instantiated every frame. Twenty-six fields read by the
native command and its `ProcessJump`, `CheckGrounded`, and `ProcessExitSwim` callees
are refreshed with direct typed property access before each eligible command.
This includes the native speed baseline, so repeated calls do not compound the
boost and baseline changes are observed. There is no reflection field-copy loop
in the movement path. Other fields retain the original clone values and are not
read by the inspected command chain; other native methods normally see the
original settings object.

The command's exception-preserving Harmony finalizer restores the original
settings reference only while it still points to this feature's copy. It preserves
an intervening settings replacement. Disable also restores an outstanding owned
reference before destroying the private copy. Shared settings assets are never
modified. A source settings identity change recreates the copy. Reentrant commands
cannot multiply the already boosted copy again.

For the complementary animation path, the feature continues to sample the
accumulator before and after `OnAnimatorMove` and multiply only the new horizontal
increment. Existing samples and vertical displacement stay unchanged. The two
changes cover the velocity/root-motion blend rather than multiplying either
component twice.

## Root-motion component

The feature samples the accumulator before and after `OnAnimatorMove` and
multiplies only the new horizontal increment. The vertical component remains the
native value. Existing accumulated samples remain unchanged, and ASKA's subsequent
collision and ground checks still execute normally. No native method is skipped.

Both callbacks use `HostedFeature.TryExecute`, which refreshes the single-player
gate and routes exceptions to the existing feature circuit breaker. Each callback
resolves the current local player and compares its movement component. The feature
does not mutate shared MovementStats assets,
attributes, animation speed, or global game time.

Boost applies only to grounded on-foot movement with nonzero movement input. Raven,
swimming, climbing, sliding, carting, rowing, and externally controlled motion are
excluded. This intentionally limits the slider to walking/running rather than
changing jump trajectories, vehicles, or scripted movement. Combat/action animation
overlap with movement input needs manual observation.

The existing range remains 1x-5x. At 1x no sample is modified. Disable/reset removes
the feature's Harmony patches and private settings copy. There is no persistent
attribute modifier. A horizontal delta already produced before disable remains a
completed movement sample and is consumed normally, just like motion from the
previous frame. Unloading or replacing a player cannot leave a speed override on
its old component.

## Verification and remaining acceptance

Eight pure Core tests pass for new horizontal distance, unchanged vertical motion,
multiple accumulation callbacks without compounding, unchanged 1x samples, invalid
multipliers, and non-finite motion rejection. They validate the arithmetic, not
runtime IL2CPP hook dispatch or the user's loaded save.

Manual acceptance on the next installed build remains required:

1. With game speed at 1x, walk and sprint between the same two markers at 1x, 2x,
   and 5x. The travel time should fall noticeably on clear level ground.
2. Return to 1x, disable, and Reset All; normal travel speed should return without
   losing any native movement/status effects.
3. Check obstacles, slopes, pauses, jump/fall, swim/climb/raven, cart/rowing, and
   combat/harvest transitions. Native collision and excluded states should remain
   normal, with no bursts after ending a pause or action.
4. Quit to menu/reload and enter a non-single-player session. The guard must stop
   callbacks, and a newly loaded character must have no residual speed override.

Five additional Core cases cover live native speed baselines, repeated
non-compounding calls, zero-speed preservation, non-finite/negative input and
overflow rejection. The parent integration owns the build and test execution for
this correction; this worker did not build or install it.

## Bounded runtime diagnostics

Each enable starts at most twelve reports, one every five seconds while the
feature is allowed. `HutchASKA.Movement` writes them to the BepInEx log and a short
sample appears below the slider. Disable/re-enable starts a new sampling window.
The extra command/root-consumption observations stop after the twelfth report.

- `commands all/local/boosted` distinguishes missing command dispatch, missing
  local identity and successful scoped speed substitution.
- `animator all/local/eligible/nonzero` distinguishes callback dispatch, identity,
  movement eligibility and actual nonzero animation displacement.
- `native/appliedSpeed`, `rootRatio` and `desiredXZ` record native speed input,
  chosen boosted input, blend share and final native desired movement magnitude.
- `blocked air/raven/swim/climb/slide/cart/row/external/idle` counts the first
  blocking condition for each local command. Input comes from the current command
  argument, not the previous command's cached `lastRawMovement`.
- `root native/added/queued` and `apply` show root-motion production and how much
  horizontal delta reaches `_ApplyRootMotion`. Queued distance is an observation
  before obstacle correction, not measured travel or proof of displacement.

Retest on clear level ground for at least ten seconds at 2x or 5x, then inspect
the visible result and these diagnostic rows. If commands are boosted with a zero
root ratio, the formerly missing velocity path is now receiving the boosted speed.
Native execution and visible travel must still be checked in-game. No live
performance or in-game success claim is made by the automated tests.
