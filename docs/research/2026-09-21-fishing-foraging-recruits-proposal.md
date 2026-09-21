# Fishing, mushroom availability and recruit rerolls

Approved controls, researched against the installed ASKA Steam build 25326768
on 2026-09-21. The user accepted this setup on 2026-09-21. The proposal itself
did not change gameplay hooks or saved game data; implementation is tracked in
[the implementation plan](../superpowers/plans/2026-09-21-gameplay-assists.md).

## Objective

Reduce repetition while completing the user's remaining fishing, cooking,
foraging and farming achievements through ordinary gameplay actions. Each new
control needs a native action test and an achievement-progress test; generated
interop signatures alone do not establish either.

## Proposed first increment: fishing

- Bite speed: Normal, 2x and 4x, affecting the local player's fishing wait only.
- Easy catch: longer reaction window and removal of the random failed-catch roll
  after a valid hooked fish. A cast, bite and reel-in remain normal player actions.
- Rare-fish boost: Normal, 2x and 4x selection weight for eligible special fish.
  These labels describe relative selection weights, not guaranteed percentages.
  Availability, bait and location rules must be resolved before implementation.

The current FishingMeleeObject exposes baseFishWaitTime, FishWaitTime,
fishEscapeTime, fishSuccessRate and FishableItems. Native _OnPullRod compares
UnityEngine.Random.value against the customized success value before its catch
branch. The success branch sends DeedsDatabase.SendDeedEvent. Preserve that
branch and its normal item/bait/proficiency processing.

FishableItemsConfig contains bonusChanceForSpecialFish and per-Fishable chance,
bait and biome data. Its selection overloads use byref parameters. Given the
observed failures of other byref detours, implementation must first establish a
narrow configuration/ordinary-call boundary and the actual weighting formula.
Do not assume the field name defines a linear probability or guarantees a rare
catch in every environment.

## Proposed second increment: mushrooms

- Mushroom regrowth: Normal, 2x and 4x frequency, restricted to positively
  identified mushroom resources.
- Additional spawn density is a separate option pending identification of the
  current world's mushroom placement/replenishment path and object limits.

The game has generic replenishment configuration (replenishFrequencyDays,
season-start rules and GetNextReplenishTime), vegetation resources, and a
GatherCondition that can score gathered quantity. Mushroom-specific asset
identity, replenishment ownership and the Mushroomed achievement's configured
condition have not yet been established. Do not patch the global replenishment
function or claim giving extra inventory items advances this achievement.
Prefer bounded, event-driven regrowth over repeated world scans or unlimited
new ground objects. Existing-world changes need separate save/reload validation.

## Proposed third increment: recruits

A manual Reroll Recruit control presents a new recruit result before summoning.
The approved first scope is starting traits/bonuses on an unsummoned candidate,
keeping the candidate's definition, name and appearance. A preview is generated
before explicitly applying the replacement traits to the upcoming choice.
Exact attribute derivation and valid perk combinations need further tracing.

VillagerOutlet exposes GenerateNewVillagerChoices and GenerateDescriptionData.
Native choice generation calls the description generator and writes the upcoming
choice array; it also invokes lost-villager selection, so it is not a traits-only
operation. DescriptionData separates definition/name/appearance from five perk
slots. A traits-only reroll must preserve the selected definition and validate
compatible generated perks, then refresh the normal preview. Do not run the
whole choice generator blindly for a traits-only request.

## Alternatives and acceptance

Recommended: separate optional gameplay assists, starting with fishing, followed
by mushroom regrowth and recruit rerolls. This permits isolated tests and gives
the normal game actions a chance to record progress.

Alternative: aggressive guaranteed rare catches, mass mushroom placement and
full candidate regeneration. These require broader changes, more performance
and persistence testing, and clearer rules about biome and candidate identity.

Before installation, validate current signatures, arithmetic, local ownership,
disable/restoration and bounded execution. In-game acceptance must confirm normal
cast/catch/bait/loot behavior, actual achievement progress, mushroom regrowth and
save/reload, and recruit preview matching the eventual summoned villager. Default
settings are normal/off. The existing 0.1.6 build-speed retest remains outstanding.
