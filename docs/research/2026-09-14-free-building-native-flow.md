# Free building: supply checks and restoration

Read-only native inspection of ASKA build 25186770 using LibCpp2IL and Capstone,
with signatures checked against the installed interop. This is implementation
evidence, not in-game acceptance. No binaries or disassembly are redistributed.

## Material and work separation

BuildPart.CheckSupplies (RVA 0xB10E90) first verifies the active layer, reads the
real container fill percentage, then checks IsFull(true). If not full, it reads
GetManifest(FULL) and tests the returned count. An empty manifest passes the
material check. The original function enables the build interaction, disables
the supply interaction, publishes the normal supply events and asks the site
to check its current layer.

BuildPart.IsSupplied (0xB129C0) uses the same IsFull/manifest-count material test.
BuildPart.IsBuilt (0xB12890) FIRST requires BuildInteraction.IsBuilt (completed
work), then evaluates the same material check. BuildSite.CheckCurrentLayer
(0xB14440) checks every part's IsBuilt before performing native layer completion
and final structure registration. The trainer never returns true from IsBuilt,
sets build volume, calls FinishPart or finalizes the site itself.

The inspected GetFillPercentage and IsFull paths do not call GetManifest, so
the first matching FULL read in each scoped method is the intended count check.

## Implemented behavior

Only those three BuildPart methods establish an ephemeral one-read scope for
their exact container. Eligibility requires a current active part, unfinished
state, active build layer, valid authoritative structure, master single-player
session and current settlement identity. The shared GetManifest postfix returns
an owned empty manifest only for the scoped FULL read. It never clears or
replaces the original persistent manifest, changes supplied item counts or
inserts synthetic materials. Scope consumption happens before later events;
exception finalizers restore any outer scope.

Existing parts receive one discovery/CheckSupplies pass after enabling. New
parts and newly activated layers already call CheckSupplies from native
StartSupply. There is no repeated full-scene scan while the feature runs.

Original build/supply interaction enabled states are retained by exact part ID,
part pointer, site pointer and structure pointer. Disable unpatches first,
restores only the interaction states the supply waiver could have changed for
unfinished matching objects, then recomputes real supplies only in a confirmed
current single-player context. Completed parts are not reverted. No native
object wrapper is retained by this restoration table.

Serialize (0xB13100) saves the REAL item container and, while building is enabled,
the ordinary current build-work value. It does not serialize the substituted
manifest or the interaction enabled flag. Deserialize (0xB11420) restores the
real container, checks supplies, then restores saved build work. Work and already
completed structures are ordinary world progress; disabling a cheat is not an
undo of construction. Existing deposited materials remain native committed stock.

## Acceptance required

- Place a normal unlocked structure with zero materials; build with the required
  tool and verify all ordinary work stages, usable completion and registration.
- Enable on a partially supplied site and a partly worked site. Counts must not
  be refunded, duplicated or consumed from unrelated inventories.
- Disable before completion: missing supplies are required again, build-work
  progress remains, and supplying the missing materials restores normal building.
- Test multi-part/multi-layer construction, upgrades, cancellation/dismantling,
  save/reload while incomplete and after completion, and scene transitions.
- Verify other settlements, absent/ambiguous ownership and multiplayer are gated.
- Check performance with many sites and inspect the supply-check counter.
