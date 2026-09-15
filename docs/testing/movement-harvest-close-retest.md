# Expanded repair retest: 0.1.3

The user reported that the 0.1.2 movement slider showed Enabled but did not change running speed, and Close did nothing while F8 and tab clicks still worked. These are native gameplay/UI failures, not resolved by the prior 115 FPS observation.

## Implementation evidence

- Movement research traced the old _moveMultiAttr through an animation-input ratio. The replacement changes the new horizontal sample from the local player's OnAnimatorMove, leaving vertical movement and the normal collision/application path intact. See [native movement evidence](../research/movement-speed-repair.md).
- Harvesting offers 1x/2x/3x/4x and uses the native gather-volume threshold plus a scoped tool-animation speed override. See [native harvesting evidence](../research/harvest-speed.md). Crop/fishing workflows are outside this first harvesting implementation.
- Close uses a fixed GUI.Button footer before entering scrolling/layout groups. The request is retained across redraws and reset only when F8 explicitly opens the menu. Both closing paths use existing input/cursor cleanup. No native click success is claimed before the user retest.
- Settings reload, Reset All and optional enabled-state restoration include harvesting; it defaults off with a 1x preset.

## Required checks

1. Confirm window version 0.1.3. Click Close on Player, Items and a disabled-controls tab; reopen with F8 each time. Check movement and cursor restoration.
2. On the same flat path, compare movement 1x, 2x and 5x. Confirm reset restores 1x, walls still block movement, and jumping/falling vertical motion is unchanged. Swimming, climbing, raven, carting, rowing and external-control modes are excluded.
3. Compare gathering at 1x, 2x, 3x and 4x, starting a fresh cycle after each change. Check normal item amounts per completion, cancellation and inventory capacity.
4. Compare tool-harvesting swing cadence at all presets. Check tool requirements, durability and stamina still follow native behavior; leave harvesting and verify ordinary movement/combat animation speed. Test 1x and Reset All while harvesting.
5. Disable/re-enable and change sessions; verify no stale animator override, multiplayer mutation or repeated errors. Check FPS with the new controls enabled.

## Additional implemented controls

- Player and tribe temperature protection prevents further cooling and frost accumulation. Warming/thawing remains native; existing cold/frost is not erased. Test entering cold/water, enabling while already cold, independent hunger/thirst, disable and owned-villager scope. [Native evidence](../research/temperature-aging-native-audit.md).
- Infinite Durability covers carried/equipped equipment's inspected decay/hit/digging wear. No Spoilage covers current carried non-equipment decay. Test partially worn tools/food, equipped and stowed tools, separate toggles, disable, repair, dropping/storage and save/reload. Neither restores condition. [Native evidence](../research/2026-09-14-item-decay-native-flow.md).
- Instant Normal Recruitment completes an already pending normal summon on its next native weather update. Test normal costs, two successive summons, population/traits/AI, disable, save/reload and no repeated spawning. The original saved deadline is never rewritten. [Native evidence](../research/instant-recruitment.md).
- All enabled tribe needs now use one shared half-second update. Test all needs on, disable one while others remain on, new recruits and reload; measure FPS with a populated tribe. Current membership is rechecked before mutation. Individual editing remains available.
- Item/villager sorting is cached until refresh/search changes. Check searching, pages, refreshed entries and cleared selection after session loss.
- Ignore Crafting Materials preserves native recipe/unlock/blueprint costs and completion. Test an unlocked recipe with zero materials, materials already in inventory/station, cancellation, disabled consumption, full inventory and saved output. Other crafting systems are not automatically covered. [Evidence](../research/2026-09-14-free-crafting-native-flow.md).
- Retain Consumables On Use preserves quantity after native effects. Test food/drink with one item and a larger stack, normal effects/animations, unrelated crafting/building spending, disable and save/reload.
- -1/+1 hour uses native clock adjustment. Test same-day steps, forward midnight day advancement, visible rejection of backward midnight, and Freeze Time remaining frozen. This changes the clock, not simulated work/needs.

The [remaining-options roadmap](../research/disabled-options-roadmap.md) records what remains unfinished. Compilation and core tests establish managed behavior, not actual IL2CPP hook dispatch or save acceptance.
