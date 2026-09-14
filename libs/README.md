# Local ASKA dependency references

This directory documents build inputs; it does not distribute dependencies. ASKA game assemblies, BepInEx-generated IL2CPP interop, and game assets must never be committed. BepInEx and other third-party runtime binaries must be installed separately and remain subject to their own licenses.

## Setup and expected files

Install the compatible BepInEx 6 IL2CPP distribution into your local ASKA installation. Launch ASKA once to allow interop generation, then exit the game. Set `ASKA_GAME_DIR` in your local environment to the folder containing `ASKA.exe`; do not commit a machine-specific path.

The plugin build resolves these files from that installation (the project file is the complete reference list):

```text
%ASKA_GAME_DIR%\BepInEx\core\BepInEx.Core.dll
%ASKA_GAME_DIR%\BepInEx\core\BepInEx.Unity.IL2CPP.dll
%ASKA_GAME_DIR%\BepInEx\core\BepInEx.Unity.Common.dll
%ASKA_GAME_DIR%\BepInEx\core\0Harmony.dll
%ASKA_GAME_DIR%\BepInEx\core\Il2CppInterop.Runtime.dll
%ASKA_GAME_DIR%\BepInEx\core\Il2CppInterop.Common.dll
%ASKA_GAME_DIR%\BepInEx\interop\Assembly-CSharp.dll
%ASKA_GAME_DIR%\BepInEx\interop\SandSailorStudio.dll
%ASKA_GAME_DIR%\BepInEx\interop\Il2Cppmscorlib.dll
%ASKA_GAME_DIR%\BepInEx\interop\Fusion.Runtime.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.CoreModule.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.IMGUIModule.dll
%ASKA_GAME_DIR%\BepInEx\interop\UnityEngine.InputLegacyModule.dll
```

`core` files come from the separately installed BepInEx distribution. `interop` files are generated locally by BepInEx from the installed game and its Unity runtime. They are reference inputs, not HutchASKA-authored output.

The initial research target is ASKA build `25186770`, Unity `6000.3.12f1`, and BepInEx `6.0.0-be.755` IL2CPP. These versions were also detected in the local Steam manifest and BepInEx startup log on 2026-09-14. HutchASKA compilation and runtime compatibility still require verification; these names do not establish compatibility by themselves.

## Build and redistribution contract

- The plugin build must fail with an actionable error if a required reference is missing. If a distribution has a different filename, report the missing expected file and inspect the actual distribution; never silently substitute an unrelated assembly.
- Every game, Unity interop, and BepInEx reference must use MSBuild `<Private>false</Private>` so it is not copied beside the plugin.
- `HutchASKA.Core` must not reference these files; its tests must run without the game.
- Reference the installation directly. Never copy game or generated interop DLLs into the public source tree, including ignored `libs/local/`.
- Installers and release packaging may copy `HutchASKA.Plugin.dll` and `HutchASKA.Core.dll`, together with project documentation, but must not copy game, generated interop, or BepInEx assemblies.
- After an ASKA update, regenerate interop when required, inspect changed signatures, and rebuild/retest against current local inputs. Do not commit old or new game DLLs to fix a build.

See the root [README](../README.md) for build/install/package commands and the [release checklist](../docs/testing/v1-release-checklist.md) for actual build/startup evidence and remaining manual acceptance checks.
