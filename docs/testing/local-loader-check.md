# Local loader check - 2026-09-14

The Stage 1 shell was built and installed with the allowlisted installation script.
Only HutchASKA.Core.dll and HutchASKA.Plugin.dll were copied.

The local Steam manifest records ASKA build **25186770**. A fresh Unity Player.log
from the normal Steam launch reports Unity **6000.3.12f1** and game version
**1.43.0809261352._PC.Release**.

The earlier BepInEx log identifies **6.0.0-be.755**, commit
**3fab71a1914132a1ce3a545caf3192da603f2258**, running **.NET 6.0.7**.
The installed assembly informational version independently matches that BepInEx
build. This historical startup is not evidence that HutchASKA loaded.

## Current limitation

**MANUAL VERIFICATION REQUIRED:** the initial direct launch exited/relaunched
through Steam. Normal Steam startup ran ASKA, but did not update the BepInEx log
or load CoreCLR. No HutchASKA startup message was observed. The local Doorstop
configuration has enabled=true and points to BepInEx.Unity.IL2CPP.dll.

No gameplay controls were exercised and no save was selected or modified by the
agent. Test game processes were closed gracefully by their observed process IDs.
F8, cursor/input behavior, feature hooks, save persistence, and session gating
remain unverified in game.

Raw game logs are deliberately not included: they can contain account information
and authentication tickets. Record only sanitized version/error evidence here.
