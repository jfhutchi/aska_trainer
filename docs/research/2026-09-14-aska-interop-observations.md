# ASKA Interop Observations — 2026-09-14

## Stage 2 context validation (2026-09-14)

### Trainer input and lifecycle validation

`SSSGame.InputManager` has `AddContext(SSSGame.InputContext.Context)` and
`RemoveContext(Context)`. Context derives from ScriptableObject and exposes
`inputMaps` (Il2CppStructArray<InputManager.InputMaps>), `priority`, `additive`,
`dontChangeCamera`, and `keepCameraFov`. The trainer owns a newly created context
with empty input maps, maximum priority, additive=false, and camera preservation.
Only that context is added/removed; native/shared contexts are never edited.
Context priority behavior, suppression coverage, cursor behavior, and native menu
interaction require manual verification. F8 uses Unity legacy keyboard polling,
independently of ASKA InputSystem maps.

Session-discovery exceptions are logged and latched after three failures so an
incompatible API cannot cause an every-frame exception loop. Native cleanup handles
are retained on restoration errors and can be retried by explicit Reset All; stale
world/player identities are discarded with a visible terminal diagnostic.
Game speed honors native zero-scale pauses, adopts a changed positive baseline after
native resume, and restores only a scale it still owns; pure controller tests cover this.

### Damage, session, and stamina signatures

### Survival semantics

Current interop confirms `GetPlayerSurvival()` returns `SSSGame.PlayerSurvival`,
whose CharacterSurvival base exposes `_foodVAttr`, `_waterVAttr`, `_warmthVAttr` as
VariableAttribute. `min` and `max` are float properties and `SetValue(float)` returns
void. Hunger/thirst fill only their own attribute to its validated native maximum.
There is no comfortable/cold/hot runtime observation yet; the generated wrapper
does not establish a safe warmth target. Temperature Immunity is Incompatible.

`PlayerCharacter.TakeDamage(SSSGame.Combat.DamageData)` returns void. The God Mode
prefix uses only this player-specific overload and compares native Unity identity to
the newly resolved local player. Session gating requires exactly one NetworkSession,
`Parameters.role == NetworkSession.Role.Singleplayer`, valid `runner`, `IsRunning`,
`IsSinglePlayer`, and neither connecting nor disconnecting. Host, Client, AutoHostClient,
and Server roles are blocked explicitly. Every callback and tick refreshes this decision.

The correct movement namespace is `SSSGame.Controllers.CharacterMovement`.
Both `Character.DrainStamina(float, SSSGame.DrainStaminaUsage)` and
`CharacterMovement.DrainStamina(float, SSSGame.DrainStaminaUsage)` return void;
`CharacterMovement.TryDrainStamina(float, bool)` returns bool. Generated wrapper IL
cannot prove the native call graph; all three entry points are covered independently,
with the movement instance compared to the local player's `GetCharacterMovement()`.
TryDrainStamina reports success when suppression applies. Native sprint/disable
behavior remains MANUAL VERIFICATION REQUIRED; no runtime acceptance is claimed.

Mono.Cecil inspection confirms `SSSGame.PlayerManager.LocalPlayer` returns its nested
`Player` object with a `playerCharacter` property typed `SSSGame.PlayerCharacter`.
The resolver requires exactly one current PlayerManager and does not cache wrappers.
`SSSGame.Weather.WeatherSystem.Instance` is a static WeatherSystem property.
Both adapters test Unity native-object validity on every resolution; scene transitions
return unavailable rather than reusing a stale native wrapper. This is signature/build
validation, not evidence that a save has been loaded or runtime behavior tested.

These notes record the game API observations made from the user's locally generated BepInEx IL2CPP interop assemblies for the current ASKA installation. They exist so implementation agents do not invent game API names when the proprietary game DLLs are unavailable in the cloud workspace.

## Compatibility context

- ASKA Steam build: 25186770
- Unity: 6000.3.12f1
- BepInEx: 6.0.0-be.755, IL2CPP
- Runtime: .NET 6.0.7
- Source assemblies inspected locally: `Assembly-CSharp.dll` and `SandSailorStudio.dll`
- The source assemblies are proprietary game artifacts and MUST NOT be committed to this repository.

## Confirmed/observed player and survival surface

`SSSGame.Character` exposes player/character health and stamina behavior including:

- `CurrentHealth`
- `MaxHealth`
- `DrainStamina(float, DrainStaminaUsage)`
- `TakeDamage(DamageData)`

`SSSGame.PlayerCharacter` exposes player-specific access including:

- `GetPlayerSurvival()`
- `GetCharacterMovement()`
- `TakeDamage(DamageData)`

`SSSGame.CharacterSurvival` exposes survival attributes including:

- `_foodVAttr`
- `_waterVAttr`
- `_warmthVAttr`
- `SuspendSurvival`

`SandSailorStudio.Attributes.VariableAttribute` exposes value APIs including:

- `GetValue()`
- `SetValue(float)`
- `min`
- `max`
- `GetNormalizedValue()`

Design implication: hunger, thirst, and warmth should use the specific variable attributes rather than globally setting `SuspendSurvival` unless no narrower safe behavior exists.

## Confirmed/observed item processing surface

Durability processing is exposed through:

- `SSSGame.ItemDurablilityProcess.Run(Item, ref float)`

Expiration/freshness processing is exposed through:

- `SandSailorStudio.Inventory.ExpirationProcess.Run(Item, ref float)`

The `SandSailorStudio.Inventory` assembly also exposes item/inventory concepts including `Item`, `ItemCollection`, and `ItemManifest`. Exact item-creation and stack-decrement methods must be verified against local references before implementing inventory mutation; do not guess them from these notes.

## Confirmed/observed crafting and building surface

Observed crafting checks include:

- `CraftInteraction.CheckOwnedRequirements()`
- `CraftInteraction._CheckOwnedBlueprintManifest()`

Construction-related types include `BuildPart`, `BuildSite`, and supply/manifest checks such as `IsSupplied` / `CheckSupplies` in the relevant construction flow.

The implementation must patch the narrow requirement/consumption path verified in the local assemblies. Do not globally suppress unrelated inventory removal.

## Confirmed/observed world-time surface

Stage 2 inspection verifies mutable bool `TimeRunningEnabled`, float `TimeSpeedMultiplier`,
`TimeOfDay`, `dayLength`, `NetworkedCurrentGameTime`, float `GetGameTimeInSeconds(float)`,
and void `SetGameTime(float)`. Their generated IL invokes native IL2CPP functions; it
does not reveal units, epoch, wrap semantics, or day-transition side effects. No save
was used for runtime observation. Thus the +/-1 hour control is Incompatible, with
no call to SetGameTime. The pure wrapping helper requires explicit caller units and
has tests for mathematical 24-hour/unit cycles; it is not wired to an assumed ASKA unit.

Freeze Time stores and restores the world's prior TimeRunningEnabled state, checking
current world identity. Global game speed uses verified Unity `Time.timeScale` get/set
and restores its captured prior scale on reset, disable, gate loss, and component unload.
Weather time acceleration is not used as global gameplay acceleration.

`SSSGame.Weather.WeatherSystem` exposes native time controls including:

- `TimeRunningEnabled`
- `TimeSpeedMultiplier`
- `SetGameTime(float)`
- `ToggleTimePassing()`

Design implication: freeze/advance/rewind world time should use these native controls rather than pausing the entire Unity process.

## Movement surface

Stage 2 reinspection: `SSSGame.Controllers.CharacterMovement._moveMultiAttr` is
`SandSailorStudio.Attributes.Attribute`. Attribute exposes `baseValue`, `GetValue`,
`SetValue(float)`, `AddModifier(AttributeModifier)` and `RemoveModifier(AttributeModifier)`.
`AttributeModifier(float, ModifierOperation)` is available and the enum has
ADD=0, MULTIPLY=1, PERCENTADD=2. Movement uses an owned MULTIPLY modifier, removed
at 1x/disable, preserving the current native baseline and unrelated modifiers.
No attribute wrapper is retained; restoration resolves the current player and checks
player instance ID plus attribute identity. A replaced/unloaded target faults visibly
instead of accessing stale native memory. Runtime speed/restoration still needs smoke testing.

The current interop assembly exposes `SSSGame.CharacterMovement` and movement-speed/sprint-related controls. The exact stable member used for the 1.0x–5.0x movement multiplier must be re-verified from the user's local current interop DLL before committing a strongly typed patch. Do not invent a field name.

## Tribe/villager surface

The current ASKA interop surface contains villager/tribe state for health and needs including food, water, warmth, happiness, rest, energy, age/lifetime, population tracking, and villager recruitment/spawn flow.

Exact member names for every tribe field were not recorded in this research note. Implementation agents MUST isolate tribe access behind an adapter and verify exact members from the user's current interop assemblies/local build feedback before finalizing strongly typed calls.

## Recruitment requirement

HutchASKA v1 must NOT create arbitrary villagers directly. It should shorten/complete the existing normal player-facing recruitment/spawner wait while allowing ASKA to perform name generation, traits, AI initialization, population registration, settlement bookkeeping, and save persistence.

Only the recruitment timer/cooldown should be altered. Unrelated tribe timers must remain native.

## Implementation rule when cloud references are unavailable

Cloud/Codex workers will not have these proprietary DLLs by default. Therefore:

1. Never add guessed game signatures just to make progress.
2. Keep pure trainer state/configuration/UI-model code independent from proprietary references and unit-test it in CI.
3. Keep game-specific hooks behind narrowly scoped adapters/patch classes.
4. Mark a game-specific hook as requiring local verification when the exact signature is not present in this document.
5. The local plugin build is authoritative for strongly typed ASKA references.
6. Do not commit ASKA or BepInEx-generated interop binaries.
# Implementation inspection: durability (2026-09-14)

Current Assembly-CSharp metadata confirms instance `void SSSGame.ItemDurablilityProcess.Run(SandSailorStudio.Inventory.Item item, ref float deltaTime)` (the spelling is native).
The by-reference parameter is named deltaTime, not durability loss. Generated IL invokes native code; it does not establish whether suppressing this interval also suppresses breakage, junk conversion, or other maintenance. No Harmony patch is installed. Infinite Durability is Incompatible until a narrow loss operation is proven. Damaged-tool freeze/resume remains MANUAL VERIFICATION REQUIRED after such a hook is established.
