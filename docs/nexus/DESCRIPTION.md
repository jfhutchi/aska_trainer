# HutchASKA - single-player trainer (0.1.20 beta)

HutchASKA adds an F8 trainer menu to ASKA for local single-player worlds. Options start off whenever a world loads. It offers player and tribe needs/protection, movement and work-speed controls, item giving, material waivers for eligible crafting/building, fishing assists, weather/time controls, and recruit trait rerolls. It does not directly set Steam achievement progress.

**Requirements:** Windows Steam ASKA and BepInEx 6 Unity IL2CPP be.755. This build was compiled against ASKA Steam build 25440748 and Unity 6000.3.12f1. Multiplayer is unsupported. BepInEx is required separately and is not included in the download.

**Install:** Close ASKA. Install BepInEx and launch the game once. Extract the ZIP into the folder containing `ASKA.exe`, keeping its directory structure. Both authored DLLs should land in `BepInEx/plugins/HutchASKA/`. Launch a single-player world and press F8. To uninstall, close ASKA and remove that plugin folder.

**0.1.20 changes:** Player-equipment Infinite Durability uses a new hook that avoids the earlier item-processing errors. It is **not yet verified in game**. The 3x bench-crafting option for player and tribe, and the Free Building exclusion for spline roads, also need fresh gameplay checks. Farm plots and spline roads require their normal materials even with Free Building enabled. No Spoilage, larger terrain leveling, mushroom-regrowth speed, and Freeze Aging are unavailable.

Many earlier controls were tested in single player during development; that does not establish compatibility for every feature in this beta or after future ASKA updates. Save before using inventory, construction, world, or villager actions. If you encounter a problem, include the ASKA version, enabled HutchASKA options, and the relevant `BepInEx/LogOutput.log` error when reporting it.

Source and issue tracker: https://github.com/jfhutchi/aska_trainer. HutchASKA is MIT licensed and includes the required notices. The mod was developed with Codex assistance and reviewed against local ASKA interop; the new 0.1.20 behavior has not been gameplay-tested.
