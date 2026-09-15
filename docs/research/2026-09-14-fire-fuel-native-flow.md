# Infinite fuel for placed fires

This feature preserves fuel already present in an owned `FireStructure`. Its first fuel supply and ignition remain normal. Empty fires are not refilled or lit. This covers placed campfires and standing lights backed by `FireStructure`; `LightOutlet.fireStructure` uses this same component.

The installed native `FireStructure.<_InitializeAttributes>b__93_3` at RVA `ABABE0` is exposed by generated interop as `__InitializeAttributes_b__93_3(float,float,float,float)`. Its full native body was inspected:

- It requires a valid network object and a burning/smoldering/blazing state. Otherwise it returns zero.
- It obtains the burn rate, applies blazing and bad-weather multipliers and wind contribution, negates that rate, and multiplies by the supplied delta time.
- The result is exclusively a fuel delta. It does not mutate fuel, stock, health, weather, fire state or work progress.
- `_InitializeAttributes` creates this callback as `_fuelConsumptionModifier` only for the master session and adds it to `_fuelVAttr`. The adjacent callback `b__93_1` changes wetness and is untouched.

The postfix replaces only finite negative consumption deltas with zero after the native calculation runs. It checks native liveness, valid state authority, master session, the owning active undismantled structure and its membership in the unique current settlement. No scene scans, persistent field writes, general attribute hooks or inventory-removal hooks are used. Disabling unpatches immediately so drain resumes from the preserved volume. Manual refueling, extinguishing, weather effects, save data and other attribute modifiers stay native.

`SSSGame.Combat.Torch` has only equipment/unequipment behavior; a separate handheld fuel-consumption path has not been established. Handheld torches are explicitly outside this feature's supported coverage. No global durability change is used as a substitute.

## Required in-game acceptance

1. Fuel and ignite an owned campfire, record fuel, enable and wait through multiple simulation updates. Confirm fuel remains steady while heat, cooking and normal effects continue.
2. Repeat with a standing torch/light. Test smoldering and blazing states where available.
3. Disable and verify ordinary fuel consumption resumes; re-enable and verify no refill or jump in stored fuel.
4. Enable with an empty or unlit fire. Verify it still needs fuel/ignition; manual refueling still consumes the supplied inventory item normally.
5. Verify rain/wind protection and manual extinguish remain effective; relighting follows native rules.
6. Verify unowned/dead/dismantled objects and other storage inventories remain unaffected. Save/reload, change sessions and check for patch errors.

Native behavior was inspected; runtime prefab coverage and these gameplay checks remain to be verified in the running game.
