# Readability and repeated-discovery repair: 0.1.2

Date: 2026-09-14. Same local ASKA build 25186770 / Unity 6000.3.12f1 / BepInEx 6.0.0-be.755 target.

## User evidence

The user's 0.1.1 screenshot shows the complete Player menu and all seven tabs, confirming that the earlier toolbar exception no longer prevents rendering. However, the window is translucent and text lacks contrast over the game scene. The user also reported approximately 10 FPS while using the trainer. The latest 0.1.1 log had no repeated trainer-render exception. No controlled baseline or profiler capture was available.

On the subsequent 0.1.2 test, the user reported approximately 115 FPS and answered the enabled-stamina question with a Player screenshot showing God Mode, Infinite Stamina, Infinite Hunger and Infinite Thirst checked. The menu reports single-player confirmed. Movement displays a saved 1.5x value with state Disabled; movement acceleration is not established by this test. The screenshot's FPS counter is cropped, so 115 FPS is the user's reported value, not an independently read measurement. This supports a substantial improvement in the tested configuration, without establishing an isolated benchmark or performance across all features.

Additional 0.1.2 screenshots show readable opaque Player, Items and Crafting & Building panels, and an item catalog containing 1,136 entries. These establish rendering and catalog population. Native cheat effects, item insertion/persistence, input restoration, large-tribe performance and scene/session transitions remain unverified.

## Changes and reasoning

- Own a cloned GUI skin with an opaque dark window, white 16px body text, 34px buttons, brighter input fields and a distinct selected-tab background. Use two rows of tabs. Normalize GUI color/content/background tint and enabled state during the trainer draw, restoring the prior state in finally. Per-feature disabled controls still use their session and compatibility gates. Textures and the cloned skin are destroyed with the trainer behaviour.
- Remove repeated whole-scene searches for NetworkSession, PlayerManager, InputManager, PopulationManager and Settlement. Previously every guard refresh scanned the scene; enabled features and stamina callbacks performed additional manager searches. The local interop declares Awake and OnDestroy on all five manager types. Cache candidates including inactive objects, invalidate through prefixes on both lifecycle methods, and check native liveness, activeInHierarchy and uniqueness on every lookup. Only activate a type's cache after both hooks install. Missing/failed hooks produce a warning and retain live discovery.
- Never cache the session decision, network role/runner state, local-player identity or villager membership. These remain live reads. Inactive candidates are retained so activation immediately changes uniqueness. Newly created managers invalidate discovery in Awake; destroyed objects are rejected by native liveness even if seen during an OnDestroy callback.
- Check whether a gameplay hotkey was pressed before refreshing its session guard. Avoid writing a survival attribute again when its value already equals its maximum.

## Automated verification and limits

The pure discovery regression makes 1,000 lookups with one discovery, then verifies immediate liveness changes. Additional tests verify invalidation on newly introduced duplicates and uniqueness changes when existing inactive candidates activate. All 69 core tests pass. Local plugin compilation passes with zero warnings/errors; the managed IMGUI scan passes for 76 reachable methods. Texture creation and the generic include-inactive discovery wrapper were inspected in the installed CoreModule; no unstripping failure stub was found in those inspected methods.

The reference to UnityEngine.TextRenderingModule is local and Private=false, just like other game references. The release contains only authored binaries and notices. The user's retest supplies the visual and reported performance evidence described above. A controlled open/closed and features-off/on comparison, lifecycle-hook behavior through transitions, and normal input on close remain outstanding. If discovery-hook warnings appear, the cache has deliberately fallen back to slower live searches and should be investigated.
