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

## Resolved loader startup

The initial direct launch exited/relaunched
through Steam. Normal Steam startup ran ASKA, but did not update the BepInEx log
or load CoreCLR. No HutchASKA startup message was observed. The local Doorstop
configuration has enabled=true and points to BepInEx.Unity.IL2CPP.dll.

Doorstop's documented native loader sets DOORSTOP_DISABLE in its process
environment. Testing the local ignore_disable_switch=true configuration option
resolved startup: CoreCLR loaded and a fresh BepInEx log on 2026-09-14 recorded
Loading [HutchASKA 0.1.0], registration of HutchASKA.Plugin.UI.TrainerBehaviour,
the blocked Unknown session state, and successful chainloader completion.
No HutchASKA exception was observed. This supports an inherited disable variable
as the loader failure cause. The local configuration change remains in place;
the original was preserved in a temporary file outside the repository.

**PASS: Stage 1 plugin discovery, IL2CPP behaviour registration, and bootstrap.**
**MANUAL VERIFICATION REQUIRED: F8 interaction and all gameplay smoke checks.**
Application.version reports 0.4 and is not the Steam build ID; use the manifest
build and full game version recorded above for compatibility reporting.

No gameplay controls were exercised and no save was selected or modified by the
agent. Test game processes were closed gracefully by their observed process IDs.
F8, cursor/input behavior, feature hooks, save persistence, and session gating
remain unverified in game.

Raw game logs are deliberately not included: they can contain account information
and authentication tickets. Record only sanitized version/error evidence here.

## Combined candidate startup

After all four implementation stages, the two authored DLLs were extracted from the local candidate ZIP into the plugin folder and launched normally through Steam. A fresh log again recorded successful HutchASKA loading and chainloader completion, all 11 expected incompatible controls, and no HutchASKA exception. Diagnostics startup now distinguishes Steam build 25186770 from application version 0.4. Unity and BepInEx matched the versions above. The main-menu session remained Unknown and blocked; no save or gameplay control was used. See the final release checklist for manual acceptance requirements.
