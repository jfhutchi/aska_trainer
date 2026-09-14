# ASKA Interop Observations — 2026-09-14

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

`SSSGame.Weather.WeatherSystem` exposes native time controls including:

- `TimeRunningEnabled`
- `TimeSpeedMultiplier`
- `SetGameTime(float)`
- `ToggleTimePassing()`

Design implication: freeze/advance/rewind world time should use these native controls rather than pausing the entire Unity process.

## Movement surface

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
