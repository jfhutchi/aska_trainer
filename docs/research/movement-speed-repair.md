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

## Replacement

The feature now samples the accumulator before and after `OnAnimatorMove` and
multiplies only the new horizontal increment. The vertical component remains the
native value. Existing accumulated samples remain unchanged, and ASKA's subsequent
collision and ground checks still execute normally. No native method is skipped.

Both callbacks use `HostedFeature.TryExecute`, which refreshes the single-player
gate and routes exceptions to the existing feature circuit breaker. Each callback
resolves the current local player and compares its movement component. The feature
does not retain game object wrappers or mutate shared MovementStats assets,
attributes, animation speed, or global game time.

Boost applies only to grounded on-foot movement with nonzero movement input. Raven,
swimming, climbing, sliding, carting, rowing, and externally controlled motion are
excluded. This intentionally limits the slider to walking/running rather than
changing jump trajectories, vehicles, or scripted movement. Combat/action animation
overlap with movement input needs manual observation.

The existing range remains 1x-5x. At 1x no sample is modified. Disable/reset removes
the feature's Harmony patch; there is no persistent attribute modifier or speed
baseline to restore. A horizontal delta already produced before disable remains a
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

No live performance or in-game success claim is made by the automated tests.
