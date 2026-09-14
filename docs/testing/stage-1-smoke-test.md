# Stage 1 smoke test

**Status: MANUAL VERIFICATION REQUIRED / LOCAL_VERIFICATION_REQUIRED.**

Subsequent coordinating-agent installation and bootstrap checks passed; see [loader evidence](local-loader-check.md) and the [final checklist](v1-release-checklist.md). The task-local record below is historical. Gameplay and F8/input acceptance remain manual.

Automated verification on 2026-09-14: 22 core tests pass; local Release plugin build passes with zero warnings and errors. Compilation uses the installed game references and does not establish in-game compatibility. No ASKA process was launched, plugin installed, or save changed by the Stage 1 bootstrap task.

Target context: ASKA Steam build 25186770, Unity 6000.3.12f1, BepInEx 6.0.0-be.755 IL2CPP. These are compatibility targets; the following runtime results have not been verified.

## Manual procedure

1. Exit ASKA before running `scripts/Install-Local.ps1`. Confirm only HutchASKA-authored DLLs are installed under `BepInEx/plugins/HutchASKA`.
2. Start ASKA normally. Confirm `BepInEx/LogOutput.log` reports HutchASKA 0.1.0, Unity and BepInEx versions without plugin registration exceptions.
3. Press F8 to open and close the trainer. Check its Close button, dragging, cursor restoration, and visibility at the chosen screen resolution.
4. Check all seven tabs: Player, Items, Crafting & Building, World, Tribe, Advanced, Diagnostics. No gameplay toggle or mutation should be available in this stage.
5. Confirm Diagnostics lists actual application/Unity/BepInEx versions, no registered gameplay features, `Session: Unknown`, and `Single-player state not confirmed`.
6. If testing a scene transition, use a disposable test session. Confirm one persistent shell remains, with no duplicate windows or exceptions. Do not use a valuable save for smoke tests.
7. Check whether mouse/keyboard input reaches game controls while the window is open. Cursor management and IMGUI event handling are present; ASKA-specific input suppression is not verified in Stage 1 and must be validated before interactive gameplay controls ship.
8. Exit ASKA; retain the relevant startup log and record the date, actual runtime versions, resolution, and pass/fail evidence here.

## Automated isolation evidence

- Unknown, multiplayer, and invalid session values fail closed with exact status reasons.
- A missing compatibility method marks only its hosted feature Incompatible.
- Session loss disables an active feature before its next tick.
- Three tick exceptions fault that feature and attempt native restoration once; an unrelated feature continues ticking.
- Failed restoration is visible in state/log callbacks and does not repeat every frame.

There is no deliberately broken gameplay hook in the shipping shell. Failure isolation is tested with pure fake features; native hook failure recovery still needs runtime checks when hooks are introduced.
