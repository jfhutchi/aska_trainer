# HutchASKA Trainer v1 Design

## Status

Design approved in chat; written specification pending user review before implementation planning.

Target repository: `jfhutchi/aska_trainer`

Initial compatibility target:

- ASKA Steam build: 25186770
- Unity: 6000.3.12f1
- BepInEx: 6.0.0-be.755, IL2CPP
- Runtime: .NET 6.0.7
- Operating mode: Windows, single-player/local play only

## 1. Purpose

HutchASKA is a local BepInEx trainer plugin for ASKA that replaces the user's dependency on time-limited external trainers while remaining maintainable across ASKA updates.

The trainer will use ASKA's generated IL2CPP interop assemblies and Harmony-style runtime patches wherever possible. It will not depend on hard-coded process addresses or pointer chains unless a future feature proves impossible through the managed IL2CPP surface.

The trainer is intended for the user's own single-player game. Multiplayer/co-op modification is out of scope for v1.

## 2. Design goals

1. Reproduce the useful WeMod-style trainer functions without a subscription or time limit.
2. Provide both an in-game F8 menu and configurable hotkeys.
3. Add ASKA-specific tribe controls beyond the external trainer feature set.
4. Use native game APIs and isolated patches so individual features can fail independently after game updates.
5. Keep Steam achievement integration untouched; the plugin must not patch or call Steam achievement APIs.
6. Avoid redistributing ASKA-owned assemblies in the public repository.
7. Make compatibility failures visible and diagnosable rather than silently applying unsafe patches.

## 3. Non-goals

The following are explicitly outside v1:

- Multiplayer/co-op support.
- Network-state manipulation.
- Steam achievement unlocking or modification.
- Bypassing Steam, DRM, anti-cheat, or other access controls.
- Reverse-engineering or modifying WeMod itself.
- Redistributing `Assembly-CSharp.dll`, `SandSailorStudio.dll`, or other ASKA game binaries.
- A separate external memory-trainer executable.
- Arbitrary manual villager creation that bypasses ASKA's normal recruitment lifecycle.

## 4. Architecture

### 4.1 Runtime model

HutchASKA will be a BepInEx IL2CPP plugin loaded into ASKA.

Primary technique order:

1. Direct calls to ASKA-generated interop classes and properties.
2. Harmony prefixes/postfixes/transpilers for narrowly scoped behavior changes.
3. Reflection or method discovery only when a stable strongly typed call is unavailable.
4. External memory manipulation is reserved as a last-resort future fallback and is not planned for v1.

The plugin must fail closed. If a method, property, or signature expected by one feature is missing, that feature becomes `INCOMPATIBLE` or `DISABLED`; the rest of the plugin continues loading.

### 4.2 Module boundaries

Proposed source layout:

```text
src/HutchASKA/
├── Plugin.cs
├── Infrastructure/
│   ├── CompatibilityService.cs
│   ├── FeatureRegistry.cs
│   ├── GameContext.cs
│   ├── Logging.cs
│   └── SinglePlayerGuard.cs
├── Player/
│   ├── PlayerCheats.cs
│   ├── SurvivalCheats.cs
│   └── MovementCheats.cs
├── Items/
│   ├── ItemCheats.cs
│   ├── ItemCatalog.cs
│   └── InventoryService.cs
├── Crafting/
│   ├── CraftingCheats.cs
│   └── BuildingCheats.cs
├── World/
│   └── WorldTimeCheats.cs
├── Tribe/
│   ├── TribeCheats.cs
│   ├── VillagerEditor.cs
│   └── RecruitmentCheats.cs
├── UI/
│   ├── TrainerWindow.cs
│   ├── Tabs/
│   └── DiagnosticsPanel.cs
├── Input/
│   └── HotkeyManager.cs
└── Configuration/
    └── TrainerConfig.cs
```

Each feature module owns only its own hooks and state. No feature should require another cheat module to be enabled.

### 4.3 Feature registry

Every trainer feature will implement a common lifecycle concept:

- `Available`: required game API/hook has been found and validated.
- `Enabled`: user has turned the cheat on.
- `Disabled`: feature is available but off.
- `Incompatible`: required API is missing or validation failed.
- `Faulted`: runtime error occurred; the feature disables itself and logs the exception.

The Diagnostics tab exposes these states.

## 5. Single-player guard

v1 must be explicitly local/single-player only.

At startup and before enabling gameplay-changing hooks, `SinglePlayerGuard` will inspect current ASKA session information through the game API.

The allowed states are explicit:

- Confirmed local/single-player session: gameplay cheats may be enabled.
- Confirmed networked/co-op session: gameplay cheats are blocked.
- Session state cannot be determined reliably: gameplay cheats are blocked and the UI reports `Single-player state not confirmed`.

An unknown state must never be treated as permission to run multiplayer-affecting cheats.

## 6. UI and input

### 6.1 Main window

Default toggle: `F8`.

Tabs:

- Player
- Items
- Crafting & Building
- World
- Tribe
- Advanced
- Diagnostics

The window must be usable with keyboard and mouse while avoiding unintended game input when the UI is active.

### 6.2 Hotkeys

Hotkeys are optional and configurable. Default assignments should be sparse to avoid collisions.

Suggested defaults:

- F1: God Mode
- F2: Infinite Stamina
- F5: Freeze Time
- F8: Show/hide trainer

Other features are menu-first unless the user configures a key.

### 6.3 Persistence

Every cheat is off by default on first install.

Configuration may optionally remember the user's selected toggles and sliders between sessions. A master setting controls whether enabled states are restored automatically.

`Reset All` restores native behavior for every active feature without requiring a game restart where technically possible.

## 7. Player features

### 7.1 God Mode

Behavior:

- Prevent incoming damage to the local player.
- Do not set health to an artificial extreme value.
- Do not alter maximum health.
- Turning God Mode off immediately returns damage handling to ASKA's native behavior.

Primary target observed in the current interop assembly: player/character damage handling such as `TakeDamage(...)`.

### 7.2 Infinite stamina

Behavior:

- Prevent or immediately neutralize stamina drain.
- Preserve the game's normal maximum stamina.
- Disabling returns stamina behavior to native operation.

Current interop exposes stamina-drain methods suitable for a narrow patch.

### 7.3 Hunger and thirst

Behavior:

- Separate toggles for hunger and thirst.
- Maintain each underlying ASKA variable attribute at its own game-defined maximum.
- Do not globally suspend all survival processing.

The current `SandSailorStudio` interop exposes variable-attribute value accessors including value, minimum, maximum, and normalized value APIs.

### 7.4 Temperature immunity

Behavior:

- Neutralize harmful temperature/warmth effects for the player while leaving unrelated survival systems active.
- Prefer maintaining the relevant warmth/temperature attribute in a safe range over globally disabling survival.

### 7.5 Movement speed

Behavior:

- User-adjustable multiplier.
- Initial supported range: 1.0x through 5.0x.
- 1.0x must restore the game's current native movement calculation rather than a hard-coded base speed.
- Movement speed is separate from global game speed.

## 8. Item and inventory features

### 8.1 Infinite durability

Behavior:

- Stop future durability loss while enabled.
- Do not automatically repair already damaged items merely by enabling the toggle.
- A separate repair-current-item action may be added later if the API supports it cleanly; it is not required for v1 acceptance.

Current interop exposes durability-processing behavior suitable for interception.

### 8.2 No spoilage / infinite freshness

Behavior:

- Prevent freshness/expiration decay while enabled.
- Preserve existing freshness values rather than setting arbitrary values unless required by ASKA's implementation.

Current inventory interop exposes expiration processing suitable for interception.

### 8.3 Add Items On Use

Defined behavior for v1:

- Using or consuming an item does not reduce its stack quantity.
- This is a retention/no-decrement feature, not a generic duplicate-on-click action.
- Arbitrary item creation belongs to the Item Browser.

### 8.4 Item Browser

The Item Browser will enumerate ASKA's runtime item definitions rather than maintain a manually copied item list.

Capabilities:

- Search by display/internal name where available.
- Select an item.
- Choose quantity.
- Add the item to the local player's inventory through ASKA's normal inventory API where possible.
- Surface failures such as invalid definitions or full inventory.

The implementation must avoid creating malformed item instances outside ASKA's normal initialization path.

## 9. Crafting and building

### 9.1 Ignore crafting materials

Behavior:

- Allow crafting when required ingredients are unavailable.
- Prevent ingredient consumption for free crafting.
- Keep the actual crafting action, produced item, and game-side completion flow native.

The current assembly exposes requirement-checking paths that can be patched narrowly.

### 9.2 Free building

Behavior:

- Construction requirements are treated as supplied while enabled.
- Building placement, completion, registration, and save behavior still go through ASKA's normal systems.

### 9.3 Free repairs

Behavior:

- Repair requirements are treated as available without consuming resources.
- Repair mechanics themselves remain native.

### 9.4 Blueprint requirements

Blueprint-requirement bypass is an optional advanced feature, not a v1 acceptance requirement.

If current API validation confirms a safe, narrowly scoped hook, the Advanced tab may expose a separate toggle to ignore blueprint requirement checks. If the hook is not stable on the target build, the feature is omitted rather than implemented through an unsafe broad patch.

## 10. World and time controls

### 10.1 Freeze time

Use ASKA's own world-time running state when available.

Behavior:

- Pause world-time progression.
- Do not pause the entire Unity process.
- Turning it off resumes normal ASKA world-time behavior.

### 10.2 Time adjustment

Required controls:

- -1 hour
- +1 hour

A direct time-of-day field may be added only if native API semantics are confirmed; it is not required for v1 acceptance.

Use ASKA's native world-time setter rather than manually editing unrelated clocks.

### 10.3 Game speed

Expose a global game-speed multiplier independently from player movement speed.

Initial presets:

- 0.5x
- 1.0x
- 2.0x
- 5.0x

Resetting to 1.0x must restore normal speed.

## 11. Tribe and villager features

The Tribe tab contains both global tribe cheats and an individual villager editor.

### 11.1 Global tribe controls

Planned toggles:

- Invincible Villagers
- No Hunger
- No Thirst
- Temperature Immunity
- Infinite Energy
- Full Rest
- Max Happiness
- Freeze Aging

One-shot actions:

- Heal Entire Tribe
- Restore All Needs

Global toggles continuously maintain the selected behavior for all currently registered tribe members and must also apply to villagers who become registered while the toggle remains enabled.

### 11.2 Villager invincibility

Use the same behavioral principle as player God Mode:

- Block incoming damage.
- Do not inflate health to arbitrary values.
- Disabling restores native damage handling.

### 11.3 Individual villager editor

The editor will select from currently registered villagers.

Editable fields, subject to validated native APIs:

- Health
- Food
- Water
- Warmth
- Energy
- Rest
- Happiness
- Age

Actions:

- Heal
- Max Needs
- Apply Changes

Individual edits are one-time changes unless a corresponding global toggle is enabled.

The editor must not retain unsafe object references across villager unload/despawn events. It should resolve the selected villager from a stable game identity whenever practical.

### 11.4 Instant villager recruitment

There will be no generic `Spawn Villager` button in v1.

Instead, HutchASKA will alter ASKA's normal villager recruitment/spawner wait so that the existing recruitment process completes immediately or at the minimum safe duration accepted by the game.

This preserves ASKA's own lifecycle for:

- name generation
- traits
- population registration
- AI initialization
- settlement bookkeeping
- save persistence

The implementation must target only the recruitment/spawner timer. It must not globally accelerate unrelated tribe timers.

If multiple recruitment mechanisms exist, v1 targets the normal player-facing recruitment path. Other recruitment paths remain native unless they are proven to use the same safe hook.

## 12. Advanced tab

Advanced controls may include:

- Restore enabled states on launch
- Reload configuration
- Re-scan runtime compatibility
- Restore native values / Reset All
- Diagnostic logging level

Riskier or less-proven features belong here rather than in the main tabs.

No experimental feature should silently enable itself.

## 13. Compatibility and update strategy

ASKA is actively updated, so update resilience is a first-class requirement.

### 13.1 No fixed offsets by default

The trainer will avoid absolute process addresses and pointer chains for v1.

### 13.2 Startup validation

Before a feature registers a runtime patch, it validates the expected type and method/property signature.

A missing or changed signature produces an explicit compatibility result instead of a crash.

### 13.3 Interop regeneration

When ASKA updates and BepInEx interop assemblies are regenerated, HutchASKA can be rebuilt against the new local references without changing repository history to include proprietary assemblies.

### 13.4 Version reporting

The Diagnostics tab and log should report at minimum:

- HutchASKA version
- ASKA build/version information when detectable
- Unity version
- BepInEx version
- Compatibility state per feature

Release notes must identify the ASKA build against which that release was tested.

## 14. Error handling

Rules:

1. A feature exception must not take down the plugin.
2. A patch failure disables only that feature.
3. Repeated runtime exceptions from a feature trip a circuit breaker and mark it `Faulted` for the session.
4. Logs should include the feature name, expected target, actual failure, and ASKA version/build information where available.
5. The UI should show a concise human-readable failure reason without dumping stack traces into the trainer window.
6. Full diagnostic detail belongs in the BepInEx log.

## 15. Steam achievements

HutchASKA will not hook, patch, suppress, unlock, or otherwise interact with Steam achievement APIs.

The trainer cannot guarantee that future ASKA releases will never change achievement behavior when mods are installed, but HutchASKA itself will make no achievement-related changes.

Cheats may naturally make gameplay achievements easier to obtain. That is distinct from directly modifying achievement state.

## 16. Configuration

BepInEx configuration will store:

- UI hotkey
- per-feature hotkeys
- default slider values
- movement multiplier
- game-speed multiplier
- whether active cheat states persist across launches
- logging verbosity

Game save files are not used as trainer configuration storage.

## 17. Repository and dependency handling

Target structure:

```text
aska_trainer/
├── src/
│   └── HutchASKA/
├── tests/
├── docs/
│   └── superpowers/
│       └── specs/
├── libs/
│   └── README.md
├── .github/
│   └── workflows/
├── .gitignore
├── README.md
├── CHANGELOG.md
└── HutchASKA.sln
```

The repository is public. Therefore:

- No ASKA DLLs are committed.
- No BepInEx binary bundle is committed unless its license and redistribution terms are explicitly suitable.
- `libs/README.md` documents which local references are required and where they come from.
- `.gitignore` excludes local copied game DLLs, build output, generated interop files, and user-specific paths.

## 18. Build model

The solution will build the plugin DLL from source while resolving ASKA/BepInEx references from a local, ignored dependency directory or configurable environment/property path.

The build must fail with a clear message when required local references are missing.

The design supports a future helper script that copies required references from the user's ASKA installation without committing them. That helper is not required for the first implementation milestone unless needed to make local builds reproducible.

## 19. Testing strategy

### 19.1 Unit tests

Unit tests cover code that can be isolated from the game runtime, including:

- feature state transitions
- config parsing/defaults
- hotkey mapping
- multiplier bounds
- reset behavior
- compatibility-result aggregation
- circuit-breaker behavior
- search/filter logic for item and villager lists where abstraction permits

### 19.2 Runtime integration checks

Because IL2CPP game objects cannot be fully unit-tested outside ASKA, the plugin provides runtime diagnostics for:

- target type found
- target method/property found
- patch successfully applied
- current feature state
- last error

### 19.3 Manual in-game smoke tests

Each release should verify at minimum:

1. ASKA starts with HutchASKA installed.
2. F8 opens/closes the UI.
3. All cheats begin disabled unless persistence is explicitly enabled.
4. God Mode blocks player damage and restores native damage when disabled.
5. Stamina, hunger, thirst, and temperature toggles can be independently enabled/disabled.
6. Durability and freshness behavior return to native state after disabling.
7. Item use retention does not corrupt stack state.
8. Item Browser-created items survive normal inventory/save behavior.
9. Crafting/building/repair cheats use normal completion flows.
10. Time controls remain separate from movement speed.
11. Global tribe toggles apply to existing and newly registered villagers.
12. Individual villager edits affect only the selected villager.
13. Instant recruitment completes the normal recruitment lifecycle rather than creating an unregistered NPC.
14. Reset All restores native behavior.
15. A deliberately unavailable diagnostic feature does not prevent plugin startup.

## 20. Release strategy

Initial semantic version: `0.1.0`.

A release should state:

- HutchASKA version
- tested ASKA build
- tested BepInEx build
- known incompatible features
- installation path
- upgrade notes

Example:

```text
HutchASKA v0.1.0
Tested with ASKA build 25186770
BepInEx 6.0.0-be.755
```

## 21. v1 acceptance criteria

v1 is complete when:

1. The plugin loads reliably under the target ASKA/BepInEx build.
2. It enforces its single-player/local-only scope, including blocking cheats when session state cannot be confirmed.
3. F8 opens a tabbed trainer UI and hotkeys work.
4. Player God Mode, stamina, hunger, thirst, temperature, and movement controls work independently.
5. Durability, freshness, item-retention, and item-browser functionality work without save/inventory corruption in smoke testing.
6. Free crafting, building, and repairs preserve normal game completion flows.
7. World time freeze/adjustment and game speed controls work independently.
8. Tribe-wide health/needs controls work.
9. The individual villager editor works for supported fields.
10. Instant villager recruitment accelerates the native recruitment process instead of directly instantiating arbitrary villagers.
11. Reset All restores native behavior where technically possible.
12. One intentionally broken/absent hook can be handled without breaking unrelated features.
13. No ASKA-owned DLLs are committed to the repository.
14. Steam achievement APIs remain untouched.

## 22. Implementation principle

Prefer the narrowest stable hook that changes only the requested behavior. Do not globally suppress a subsystem when a specific ASKA attribute or method can be controlled directly. Preserve ASKA's native lifecycle for object creation, inventory registration, construction, villagers, saves, and world state whenever possible.
