HutchASKA 0.1.20 - beta build

Single-player in-game trainer for the Windows Steam version of ASKA.
This is an unofficial community mod. It does not include ASKA or BepInEx files.

REQUIREMENTS

- ASKA for Windows. The code was built against Steam build 25440748.
- BepInEx 6 Unity IL2CPP build be.755. Install and launch it once before adding HutchASKA.
- A local single-player game. Multiplayer is unsupported.

INSTALL

1. Close ASKA.
2. Install BepInEx 6 Unity IL2CPP separately and launch ASKA once to generate interop files.
3. Extract this ZIP into the ASKA game folder, the folder containing ASKA.exe.
4. Confirm these two files exist:
   ASKA/BepInEx/plugins/HutchASKA/HutchASKA.Plugin.dll
   ASKA/BepInEx/plugins/HutchASKA/HutchASKA.Core.dll
5. Launch ASKA, load a single-player world, and press F8.

All trainer options start off each time a world loads. The menu includes player,
item, crafting/building, world, fishing/foraging, tribe, recruitment, advanced,
and diagnostics controls.

CURRENT LIMITS

- New in 0.1.20: Infinite Durability for the player's carried/equipped equipment
  is implemented but has not been tested in the game. Existing damage is not repaired.
- The 3x player/tribe bench crafting option and the spline-road Free Building
  exclusion also need fresh in-game checks.
- Free Building does not waive farm-plot or spline-road materials.
- No Spoilage, expanded terrain leveling, mushroom regrowth speed, and Freeze
  Aging remain unavailable. Their disabled controls explain why in the menu.
- Game updates may require a new build. Save before using inventory, world,
  construction, or villager actions.

UNINSTALL

Close ASKA, then remove BepInEx/plugins/HutchASKA. Removing the mod does not
undo normal game progress already saved after trainer actions.

Source and issue reports: https://github.com/jfhutchi/aska_trainer
License: MIT. See LICENSE, THIRD_PARTY_NOTICES.md, and licenses/ in this folder.
