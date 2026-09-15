# One-hour clock adjustment native audit - 2026-09-14

## Evidence

Read-only inspection of the local ASKA Steam build 25186770, Unity 6000.3.12f1,
using generated interop signatures, bundled LibCpp2IL metadata mappings and
Capstone native disassembly. Raw binary/disassembly artifacts remain outside the
repository. No game, save, installation or binary was changed during this audit.

`WeatherSystem.TimeOfDay` uses hours, with a normal range of [0, 24). The native
setter divides/repeats by the verified floating-point constant 24. For an input
at least 24 it also adds the integer day carry to `dayOfYear`. For a negative
input it wraps the hour but does **not** decrement the day. Consequently, passing
-0.5 at 00:30 would produce 23:30 on the same day, not the previous day.

`WeatherSystem.SetGameTime(float)` calls `Rpc_SetGameTime(float)`. Its local state
authority branch calls the native hour setter, sets `_forceUpdatePending` and
clears `_initDone`. `LateUpdate` subsequently runs `_Presetup`; the forced
`_UpdateTime` path refreshes the clock/weather state with `DeltaTimeOfDay = 0`.
`AfterAllTicks` synchronizes the absolute clock as day plus hour divided by 24.
This is a clock adjustment, not a simulation of one hour of every gameplay system.

Neither `SetGameTime` nor this refresh changes `TimeRunningEnabled`. Freeze Time
therefore remains in effect at the selected hour. The forced refresh also runs
when normal time progression is stopped.

The normal `_CheckNightDayTransition` path uses `_lastSunriseDay` and
`_lastNighfallDay` to avoid repeating their respective daily events. `OnNewDay`
is raised at sunrise, not simply at midnight. The trainer does not manually
raise events, write day/year counters or reset the native event bookkeeping.

## Implementation boundaries

- Accept only one-hour forward/backward actions and a finite current hour in
  [0, 24). Reject backward steps across midnight with a visible reason.
- Pass forward targets at or above 24 directly to `SetGameTime`, preserving its
  native day carry. At 23:30, pass 24.5, not 0.5.
- Require a current world and completed native initialization. A second action
  while refresh is pending is rejected until the clock is ready again.
- Execute through the existing guarded action host and use only the native
  `SetGameTime` API to mutate the clock. Read back day/hour to confirm the local
  authority applied the requested value. Do not retry a mismatched result.
- Preserve native freeze, weather and event processing; no direct field writes,
  guessed timer units or rewinding the year.

## Verification and remaining live checks

The parent integration build passed with zero warnings and all 120 Core tests
passed, including 14 hour-boundary/direction cases. These verify target arithmetic
and rejection behavior; they do not execute the IL2CPP game.

Manual acceptance remains: adjust both directions during a normal day, advance
23:30 to next-day 00:30, reject 00:30 backward without changing time, repeat while
Freeze Time is active, and observe native lighting/weather and sunrise behavior.
No live-game effectiveness claim is made from static tracing or unit tests.
