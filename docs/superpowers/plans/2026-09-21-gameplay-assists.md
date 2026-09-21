# Gameplay Assists Implementation Plan

> **For agentic workers:** Use subagent-driven-development for isolated research/feature tasks. Root owns shared wiring, review, build and installation.

**Goal:** Implement the approved fishing, mushroom-regrowth and traits-only recruit-reroll controls using normal ASKA gameplay paths.

**Architecture:** Isolate each subsystem behind the existing NativeFeature/HostedFeature gates. Native research must establish current-build semantics before choosing hooks; no raw offsets, global randomness patches, primitive-byref detours, or achievement-state writes. Root connects the features to configuration and the existing menu, then packages 0.1.7.

**Tech Stack:** C#/.NET 6, BepInEx IL2CPP, HarmonyX, xUnit, PowerShell; local Mono.Cecil/LibCpp2IL/Capstone for read-only inspection.

## Task 1: Fishing

- [ ] Inspect current bite wait, escape, catch success and rare selection formulas; record evidence in docs/research/2026-09-21-fishing-native-flow.md.
- [ ] Create src/HutchASKA.Plugin/Player/FishingAssistFeature.cs with local-player ownership, normal/2x/4x bite and rarity settings, easy catch, reversible enable/disable and bounded diagnostics. Scale eligible rarity weights, preserving bait/biome/availability and normal successful catch events.
- [ ] Add pure arithmetic/selection tests under tests/HutchASKA.Core.Tests/Player and helpers under src/HutchASKA.Core/Player as justified by the native contract. Verify invalid/zero weights, normalization and restoration, not just implementation-shaped snapshots.
- [ ] Review spec coverage, then native safety/correctness before integration.

## Task 2: Mushroom regrowth

- [ ] Identify mushroom assets and current replenishment scheduling; record positive identifiers and exact scope in docs/research/2026-09-21-mushroom-regrowth.md. Keep raw game assets outside the repository.
- [ ] Create src/HutchASKA.Plugin/World/MushroomRegrowthFeature.cs with normal/2x/4x frequency, mushroom-only scope, natural eligibility and bounded work. Do not alter generic resource replenishment globally or spawn unlimited objects.
- [ ] Add meaningful scheduling/identity tests under tests/HutchASKA.Core.Tests/World with any pure helpers in src/HutchASKA.Core/World.
- [ ] Review spec coverage and ownership/scheduling/cleanup. If an exact contract cannot be established, document the concrete limitation instead of installing a speculative hook.

## Task 3: Recruit rerolls

- [ ] Trace candidate generation, perk compatibility, pending summon state, previews and native synchronization; document in docs/research/2026-09-21-recruit-reroll.md.
- [ ] Create src/HutchASKA.Plugin/Tribe/RecruitRerollFeature.cs and an isolated UI/Tabs/RecruitRerollPanel.cs. Reroll only candidate traits/starting bonuses while retaining definition, name and appearance; expose a result before normal summoning. Resolve live owned outlet/choice on each action; no edits to established villagers or lost-villager selection.
- [ ] Add pure identity/stale-choice/perk-validation tests under tests/HutchASKA.Core.Tests/Tribe as dictated by the native contract.
- [ ] Review spec coverage, stale-state handling and native preview/persistence consistency.

## Task 4: Integration and delivery

- [ ] Root edits Plugin.cs, RuntimeConfiguration.cs, TrainerWindow.cs and appropriate tabs to register/wire completed features. Defaults remain normal/off; Reset All/config reload/rescan follow existing behavior. Manual rerolls are not replayed by restored toggle configuration.
- [ ] Run dotnet test tests/HutchASKA.Core.Tests/HutchASKA.Core.Tests.csproj -c Release and scripts/Build-Local.ps1 against the installed ASKA directory. Expected: no test failures, build warnings/errors or interop-gate failures.
- [ ] Review combined changes independently, fix actionable defects, and update README, CHANGELOG and acceptance evidence with actual supported behavior and outstanding gameplay checks.
- [ ] Commit coherent source, package using scripts/Package-Release.ps1, and inspect the strict release file list. Install only while ASKA is closed and verify installed versions/hashes. No game launch, save edits or achievement unlocks are automated.
- [ ] Provide short first-test instructions for casts/catches, eligible mushroom regrowth and recruit preview; retain the outstanding 0.1.6 construction test separately.

## Approval

The user approved the proposed setup on 2026-09-21 ("sounds good I agree"). Use traits-only recruit rerolls as the proposed default. Additional world spawn density is outside this increment.
