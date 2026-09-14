# Stage 2 Player/World verification

Date: 2026-09-14. Local compile target: ASKA Steam build 25186770, Unity
6000.3.12f1, BepInEx 6.0.0-be.755 IL2CPP, runtime .NET 6.0.7.

**Implementation complete; gameplay acceptance pending. MANUAL VERIFICATION REQUIRED.**
The user authorized continued implementation despite manual test gaps. No disposable
save was opened or gameplay input sent by the implementation worker. The Stage 2 exit
criteria remain unmet until the manual checks below pass. No runtime-compatible release
is claimed. Stage 1 loader evidence is recorded separately in [local-loader-check.md](local-loader-check.md).

## Automated and build evidence

- `dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj`: PASS, 38 tests.
- `scripts/Build-Local.ps1`: PASS, 0 warnings, 0 errors, against current local interop.
- `git ls-files '*.dll' '*.exe'`: no tracked binaries.
- Required native members were inspected with local Mono.Cecil; research records exact signatures and uncertain semantics.
- Installation/boot checking belongs to the coordinating agent; see its loader record for the exact installed version/evidence. The implementation worker did not install or launch ASKA.
- Review follow-up: three pure input lifecycle tests cover menu recovery after session confirmation, hotkey reactivation after a blocked session, and terminal failure isolation after a partial enable. They do not establish native input-context behavior.

## Exact smoke matrix

Use a disposable/backed-up single-player save and confirm each enabled control can be
disabled independently. Never treat compilation or an old loader log as a runtime pass.

| # | Check | Current result |
| --- | --- | --- |
| 1 | F8 opens/closes; prior cursor lock/visibility return; owned input context suppresses gameplay only while open | MANUAL VERIFICATION REQUIRED |
| 2 | God Mode blocks player damage; off restores damage; unrelated characters still take damage | MANUAL VERIFICATION REQUIRED |
| 3 | Stamina does not drain through sprint/other drain paths; off restores drain | MANUAL VERIFICATION REQUIRED |
| 4 | Hunger and thirst hold at their native maxima independently; unrelated survival remains active | MANUAL VERIFICATION REQUIRED |
| 5 | Temperature immunity acts safely or reports Incompatible without affecting other features | Incompatible by design; safe warmth target unverified. UI/runtime isolation MANUAL VERIFICATION REQUIRED |
| 6 | Movement 2x is visibly faster; 1x/disable removes trainer modifier and restores current native calculation | MANUAL VERIFICATION REQUIRED |
| 7 | Freeze Time halts only the world clock and restores prior running state | MANUAL VERIFICATION REQUIRED |
| 8 | +/-1 hour works across day boundaries | Incompatible by design; native units/day boundary behavior unverified; no setter call implemented |
| 9 | 0.5x/1x/2x/5x game-speed presets restore correctly; native pause/menu/unpause works | MANUAL VERIFICATION REQUIRED; pure pause/baseline tests PASS |
| 10 | Incompatible-feature simulation does not crash plugin or disable unrelated features | Core isolation tests PASS; runtime UI/patch simulation MANUAL VERIFICATION REQUIRED |
| 11 | BepInEx log contains no repeating exception loop during gameplay and toggles | MANUAL VERIFICATION REQUIRED; feature breaker/session discovery limits implemented |

## Additional acceptance checks

- Every cheat off on first installation and when restore-enabled-states is false.
- F1/F2/F5 configured routing; duplicate bindings ignored; F8 stays usable when blocked.
- Unknown/connecting/disconnecting/host/client sessions block every mutation and callback.
- Loading another scene/world/player while active cannot write through old interop wrappers.
- Reset All restores active modifiers/clocks; failed cleanup can be explicitly retried without a frame-by-frame exception loop.
- No Unity time-scale change leaks after gate loss or trainer component disable.
- Menu input context is removed on close, component disable, and safe manager transitions.
- Keep F8 open while entering a single-player session and across a temporary loss of session confirmation; gameplay suppression must resume without closing/reopening the trainer. Previously blocked F1/F2/F5 features must respond again to an explicit key press after single-player confirmation.
- A failed native input-context installation must reach the feature's bounded failure/cleanup path, never appear successfully installed merely because a context was allocated. Native fault injection remains MANUAL VERIFICATION REQUIRED.
- Opt-in restoration waits for positive single-player confirmation; configuration never silently enables cheats by default.

All additional checks are MANUAL VERIFICATION REQUIRED unless a specific automated test
is cited. Temperature/hour incompatibility is intentional and does not constitute a
runtime pass for those controls.
