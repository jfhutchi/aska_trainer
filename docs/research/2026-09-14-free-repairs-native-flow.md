# Free repairs: outstanding materials

Local native analysis of the installed ASKA GameAssembly and matching generated interop established this narrow transaction. No game binaries or raw disassembly are included.

- `RepairPart._RefreshPartsStatus` (RVA `B3E840`) checks initialized state and the existing health/repair threshold, then calls its storage container's `GetFillRatio(true)`. A ratio of at least one calls the normal `CurrentStage = Repair` transition.
- `RepairPart.set_CurrentStage` (RVA `B3F700`) requires master authority. Leaving Supply clears the network repair manifest. Entering Repair initializes the normal remaining healing budget from maximum health minus current health.
- `OnNetworkManifestChanged` (RVA `B3D070`) processes the manifest difference and removes already supplied items through the normal repair container. The trainer does not suppress that removal, refund stock, insert items, or change the stored demand manifest.
- `_OnHittableHit` (RVA `B3E270`) applies the lesser of remaining healing and normal hit work. `_RefreshRepairsStatus` continues to govern later stages. The trainer does not set health, completion, stage, threshold, or repair speed.
- `_ResetRepairsManifest` already treats an empty native cost as permission to enter Repair. This supports waiving outstanding material qualification while retaining the normal work stage.

The override changes only `GetFillRatio(true)` for the exact repair container during that repair's `_RefreshPartsStatus` call. It is consumed once and restored by a finalizer. Other container/UI queries keep their actual values. The part must be initialized, active, in Supply, part of the unique current settlement, and backed by a valid master-authoritative network object; dead, dismantled and salvage structures are excluded.

Existing repairs are discovered once after enabling. Initialization, repair status and network stage callbacks cover later repairs without a per-frame scene search. Disabling removes all hooks. Already granted repair work remains valid, like already paid supplies; disabling does not undo repaired health or revoke that budget. Further repair cycles require normal materials after disabling.

## Required in-game acceptance

1. Damage an owned structure below the normal repair threshold with zero supplies; enable, verify Repair becomes available, inventory counts remain unchanged, and hammer work is still required.
2. Partly supply a repair first; enable and verify outstanding materials are waived, previously deposited items are consumed normally exactly once, and nothing is refunded or duplicated.
3. Enable before damaging a structure and before loading/initializing another structure; verify the next Supply stage transitions and no repeated completion/FX occurs.
4. Disable before a new repair cycle and verify normal supplies are needed. Disable during an already granted repair and verify its work budget remains normal.
5. Verify intact, destroyed/salvage, dismantled and unowned structures are unaffected; containers outside the scoped repair call report real capacity and contents.
6. Save/reload after grant and after partial repair; verify normal persistent stage/work state and no replayed resource deductions. Check log for patch errors and exercise multiplayer/session gating.

Native flow was inspected; these acceptance checks require the game and are not replaced by a successful plugin build.
