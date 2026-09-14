# GUI rendering repair: 0.1.1

Date: 2026-09-14. Target: ASKA Steam build 25186770, Unity 6000.3.12f1, BepInEx 6.0.0-be.755 and Il2CppInterop 1.5.1-ci.829.

## Reported failure and local evidence

The user's 0.1.0 log shows repeated `System.NotSupportedException: Method unstripping failed` from `GUIContent.Temp(Il2CppStringArray)` through `GUILayout.Toolbar` and `TrainerWindow.DrawContents`. IL2CPP catches the exception at its managed-delegate trampoline, so the outer `OnGUI` caller does not receive it and rendering repeats the failure.

Mono.Cecil inspection of the locally installed `UnityEngine.IMGUIModule.dll` confirms two unconditional throwing stubs: the array conversion helper and the final `GUILayout.Toolbar` overload with GUIContent, enabled entries, four styles, button size and options. Supplying GUIContent objects alone would still reach the second stub. Individual string Button/Label paths use available native wrappers.

The log also contains a separate game backend `LoginWithSteam` / `EvaluationModePlayerCountExceeded` error. This repair addresses the HutchASKA rendering stack; no backend-login fix is claimed.

## Change

Use a horizontal row of regular buttons with the selected tab bracketed. Tab changes reset scrolling. Share a one-failure callback guard between the outer draw and the native window delegate. Catch inside the delegate, restore GUI.enabled, log the first fault, and return a failed draw even when the native window returns normally. TrainerBehaviour closes using the existing menu-input/cursor restoration path and refuses reopening a faulted renderer until restart.

## Verification

- Before the UI fix, `scripts/Test-LocalGuiInterop.ps1` failed against the compiled 0.1.0 plugin with both reachable stubs and their call paths.
- After the fix, it passes for 22 reachable managed IMGUI methods. The check follows direct method references within the local IMGUI assembly; native execution, dynamic dispatch and implicit type initializers are outside its scope.
- Callback tests cover first-failure containment, one report across repeated attempts, healthy repeated renders, and a nested delegate failure followed by a normal native return. All 66 Release core tests pass.
- Release plugin compilation against the installed game references passes with zero warnings/errors. Both authored assemblies and the plugin metadata are version 0.1.1.
- Local build/install/package helpers now run the interop check after building. No game/interop DLL is copied into source or the release archive.

## Required in-game retest

No native F8 interaction is claimed by this repair's automated checks. Launch with 0.1.1 and check F8, all seven tabs, scrolling, dragging and Close; then confirm cursor and gameplay input restoration. Check a fresh log for rendering errors. Gameplay feature acceptance remains governed by the release checklist.
