# Local player building speed

Build Speed provides the requested 1x, 2x, 3x and 4x presets by scaling construction progress per normal player work event. It is independent of Free Building and does not alter material demands, unlocks, animation speed, tools, elapsed action time or automatic builders.

## Native contract

The installed `PlayerBuildInteractionConfig.PlayerBuildInteractionSession._OnAnimatorEvent` (RVA `C82D00`) checks its configured work-event name, computes work from player attributes, moveset and `_buildTime`, applies the equipped-item contribution modifier, then calls `BuildInteraction.RequestAddBuildVolume(ref float)` at RVA `C82F9B`. Later native code handles action costs, work injury, proficiency, contribution tracking, widget updates and cancellation. `_buildTime` stays unscaled.

`RequestAddBuildVolume` (RVA `AA72D0`) reads the float into the native hit request, performs normal hit processing, increments and clamps the interaction's progress, emits its progress event and calls `BuildSite.CheckCurrentLayer`. It never writes its reference parameter. Its full body was inspected before choosing the patch boundary.

The trainer establishes a thread-local scope only while the local player's running, target-matched, started build session processes its animation event. Only the first matching interaction's work request consumes the scope. Local identity, session and master status are checked again before scaling. Villager and unrelated calls have no matching scope.

The entry guards were checked against lifecycle bodies: `Start` sets session state to RUNNING and initially clears both action flags. `_OnMatchTarget` sets `_targetMatched = true` after successful positioning with the interaction input held (RVA `C834DA`). `Update` requires RUNNING and that target flag, then sets `_actionStarted = true` before it requests the building animation (RVA `C82C89`). Consequently a normal building work event reaches `_OnAnimatorEvent` with all three guards already satisfied; they are not first set inside that handler. `BuildInteraction.Awake` resolves its network session before `OnEnable` subscribes the normal master's hit handler.

The feature status reports bounded local animation-event and scaled-work-request counters, plus the latest readiness/guard reason. Text updates are sampled after the first few events; a changed guard reason is shown immediately. This is diagnostic evidence of hook execution, not a claim that gameplay acceptance passed.

Only finite positive contributions are multiplied. Zero remains zero; negative corrections remain unchanged. Invalid input, multiplier or overflow is rejected through the feature's failure guard. The temporary parameter override is restored on both success and exception before the caller continues. Scope finalizers prevent leaks across nested calls. There are no persistent native changes or scene scans; disabling or selecting 1x affects subsequent work events immediately.

## Validation

Core tests specify all four multipliers, repeated contribution restoration, zero/negative handling, nonfinite rejection and overflow. Tests were written before implementation but were not executed in the agent task because integration owns builds/test execution.

Required gameplay acceptance:

1. With the same tool and structure type, compare contribution per swing at 1x, 2x, 3x and 4x. Each should produce the corresponding work multiplier without changing animation cadence.
2. With Free Building off, verify ordinary materials and unlocks remain necessary; with it on, verify the two controls work independently.
3. Confirm normal per-swing stamina/tool/injury/proficiency behavior and completion effects, particularly when 4x crosses the remaining work threshold.
4. Change presets during a build and disable mid-action. Subsequent swings should use the new setting without compounding previous work or reverting completed progress.
5. Confirm villagers, gathering, combat and repairs are unaffected; cancel/restart, change tools, lose session access, and reload the game to check scope cleanup and gating.
