# Local player building speed

Build Speed provides the requested 1x, 2x, 3x and 4x presets by scaling construction progress per normal player work event. It is independent of Free Building and does not alter material demands, unlocks, animation speed, tools, elapsed action time or automatic builders.

## Updated binary recheck: 2026-09-21

The original RVAs in the sections below describe build 25186770. Fresh LibCpp2IL
metadata and native disassembly were generated for the now-installed build
25326768 / game 1.44.1509261752. GameAssembly SHA256:
`40AF0C536C24836775FEB55E0C3C0058C0C4C0972AAA1867EDF2ED5F5CE3EFB7`.

Current RVAs are `_OnAnimatorEvent` C841A0, `get_BuildConfig` C84A60,
`RequestAddBuildVolume` AA85A0, `AwardProfficiencyPoints` DE1630 and
`HandleWorkInjury` DE2240. The native event still obtains its moveset through the
getter, multiplies weighted attributes by moveset+0x70, adds moveset+0x60 and
multiplies by original elapsed time and tool contribution. The cached getter
still retains the original at session+0x30. Entry flags remain at +0x48/+0x49;
native matching and Update set them before the animation event. Tool wear still
uses +0x74; proficiency and injury use their separate, unchanged fields. No
native addresses are hardcoded in the implementation. This confirms the chosen
replacement boundary against current files, not live successful gameplay.

## Native contract (original build evidence)

The installed `PlayerBuildInteractionConfig.PlayerBuildInteractionSession._OnAnimatorEvent` (RVA `C82D00`) checks its configured work-event name, computes work from player attributes, moveset and `_buildTime`, applies the equipped-item contribution modifier, then calls `BuildInteraction.RequestAddBuildVolume(ref float)` at RVA `C82F9B`. Later native code handles action costs, work injury, proficiency, contribution tracking, widget updates and cancellation. `_buildTime` stays unscaled.

`RequestAddBuildVolume` (RVA `AA72D0`) reads the float into the native hit request, performs normal hit processing, increments and clamps the interaction's progress, emits its progress event and calls `BuildSite.CheckCurrentLayer`. It never writes its reference parameter. Its full body was inspected before choosing the patch boundary.

The work calculation reads the moveset's `damageMultiplier` at offset `0x70` and `baseUnarmedDamage` at offset `0x60`:

```text
work = (weightedAttributeSum * damageMultiplier + baseUnarmedDamage)
       * buildTime * equippedItemModifier
```

The replacement scales both work coefficients by the selected preset and leaves `RequestAddBuildVolume` unpatched. The inspected player call passes the address of a stack-local float, not a null pointer. The original event calculates and submits the work normally. Scaling `_buildTime` instead would also change tool wear and proficiency, so elapsed time remains native.

## Private configuration scope

Both `PlayerBuildInteractionConfig` and `InteractionMoveset` derive from `UnityEngine.ScriptableObject`. Each eligible local work event creates a private clone of each object and connects the private moveset to the private configuration. Only the two work coefficients change. Neither shared asset nor the session's configuration fields are written.

The declared `BuildConfig` getter (RVA `C835C0`) returns the cached configuration at offset `0x30`, or derives and caches the original from the base configuration at offset `0x10`. Its postfix substitutes the private configuration only while the exact owning session's work-event scope is active and only when the native getter returned that scope's original configuration. The getter has already cached the original before its result is substituted. Later native reads in the same event receive a consistent private configuration without replacing the session cache.

The native event's tool wear calculation reads the unchanged `weaponDurabilityDamage` at moveset offset `0x74`. Inspected `AwardProfficiencyPoints` (RVA `DE0190`) reads proficiency data at offsets `0x78`, `0x80`, `0x88` and `0x90`; `HandleWorkInjury` (RVA `DE0DA0`) reads data at `0x90` and `0xA0`. Neither method reads the two scaled work coefficients or retains the private moveset. Their native calls and original elapsed-time inputs remain intact.

The trainer establishes a thread-local scope only for the configured animation work event of the current local player's running, target-matched, started build session. The active interaction must still match and its network session must be master. Local identity, interaction and authority are checked again before each getter substitution. Villager and unrelated session calls do not match this scope. There is no tool-category filter.

The entry guards were checked against lifecycle bodies: `Start` sets session state to RUNNING and initially clears both action flags. `_OnMatchTarget` sets `_targetMatched = true` after successful positioning with the interaction input held (RVA `C834DA`). `Update` requires RUNNING and that target flag, then sets `_actionStarted = true` before it requests the building animation (RVA `C82C89`). Consequently a normal building work event reaches `_OnAnimatorEvent` with all three guards already satisfied; they are not first set inside that handler. `BuildInteraction.Awake` resolves its network session before `OnEnable` subscribes the normal master's hit handler.

Each event reads the original configuration before publishing its scope. Nested callbacks temporarily suspend the parent scope and restore it in their finalizer. Clone ownership is recorded before preparation so partial allocation failures can be cleaned up. An event finalizer first restores the parent scope, then releases its private objects after the native callback has finished. Disabling unpatches future calls immediately but does not destroy clones still in use by active callbacks; their finalizers perform cleanup. A failed cleanup remains visible through the hosted feature's failure handling.

`BuildWorkCoefficients` accepts finite signed native coefficients, scales both from their original values, and rejects invalid presets, nonfinite values or overflow. This preserves zero and signed terms in the native formula without compounding previous presets. Selecting 1x creates no new clone scope. Changing presets affects subsequent work events and never reverses existing progress.

Status shows bounded local work-event and boosted-event counts plus the last observed build volume before and after an event. A boosted-event count means the private configuration was substituted, not that a particular amount of progress was accepted. The first twelve observed eligible events are logged. Readiness failures are shown in the status reason. These diagnostics do not substitute for gameplay acceptance.

## Superseded direct work hook

The earlier implementation patched `RequestAddBuildVolume(ref float)` and temporarily multiplied its reference argument. In the reported 0.1.5 session, building remained at zero and the log repeatedly showed `NullReferenceException` from the native-to-managed `RequestAddBuildVolume(IntPtr, Single&, Il2CppMethodInfo*)` trampoline, without feature methods in the stack. The replacement removes that hook entirely.

Inspection found an unusual `ldind.i` conversion in the installed bridge's by-reference argument handling. That observation does not establish the exception's cause: an isolated reproduction of the conversion and float round trip succeeded on the installed runtime. The native parameter and inspected call site are valid. The exact trampoline failure remains unresolved; the replacement avoids the observed failing boundary while preserving the game's normal work submission.

## Validation

Core tests cover the work formula at all four presets, zero and signed coefficients, repeated scaling from the original values, invalid inputs and overflow. Integration reported a warning-free build and 189 passing core tests for the replacement. The independent review inspected clone ownership, nested scopes, reentrant disable, the cached native getter and the native cost/proficiency/injury paths. No gameplay result is implied by these checks.

Required gameplay acceptance:

1. With the same tool and structure type, compare contribution per swing at 1x, 2x, 3x and 4x. Each should produce the corresponding work multiplier without changing animation cadence; first confirm that progress advances and the former submission-trampoline errors stop.
2. With Free Building off, verify ordinary materials and unlocks remain necessary; with it on, verify the two controls work independently.
3. Confirm normal per-swing stamina/tool/injury/proficiency behavior and completion effects, particularly when 4x crosses the remaining work threshold.
4. Change presets during a build and disable mid-action. Subsequent swings should use the new setting without compounding previous work or reverting completed progress.
5. Confirm villagers, gathering, combat and repairs are unaffected; cancel/restart, change tools, lose session access, and reload the game to check scope cleanup and gating.
