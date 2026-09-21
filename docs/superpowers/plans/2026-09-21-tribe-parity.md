# Tribe Work and Skill Gain Implementation Plan

> Use subagent-driven-development for independent native work-speed research and implementation; root owns skill gains, shared integration and final review.

**Goal:** Implement the user's requested player/tribe skill gains and tribe harvest/build presets (normal, 2x, 3x, 4x, 5x), and audit other player controls for tribe equivalents.

**Architecture:** Use narrowly scoped native award/work callbacks with current local-player or owned-villager qualification. Keep levels, caps, materials, AI and completion paths native. Defaults are normal; configuration reload/reset and restored feature gates follow existing patterns. No periodic world scans or direct level/save edits.

**Tech Stack:** C#, .NET 6, BepInEx IL2CPP, HarmonyX, xUnit, PowerShell, read-only local native inspection.

The user explicitly requested these controls and tribe equivalents on 2026-09-21. Implement the requested numeric presets using the existing menu design. Installation is deferred until the user finishes testing and closes ASKA.

- [x] Verify and implement player/tribe experience scaling at InteractionMoveset.AwardProfficiencyPoints; check ownership, native caps, notifications and other skill-award paths. Add arithmetic boundary tests and research evidence.
- [x] Verify native villager building/harvesting/gathering methods, then implement separate tribe-wide multiplier features in src/HutchASKA.Plugin/Tribe with no changes to player work hooks. Document exact effects and unsupported paths. Add pure tests where native calculation warrants them.
- [x] Audit player feature parity against existing tribe controls; implement only verified equivalents and document concrete limits for other requested controls.
- [x] Wire feature registration, independent configuration, Player/Tribe panels, reset/reload and diagnostic reasons. Preserve pending harvest-tab/full-height/prompt-name changes.
- [x] Review source/native contracts, build with local interop, run relevant core tests and existing compiled-hook/GUI gates; record real results and outstanding runtime acceptance.
- [x] Prepare delivery record. Installation and the separately requested rare-fish 20x/30x/40x/50x update remain deferred until ASKA closes, as requested.

## Prepared delivery

Player/tribe skill gains, tribe build/harvest and verified tribe ground movement are implemented and independently reviewed. Shared wiring, reset/reload and existing pending UI changes are complete. Final local build passed with zero warnings/errors, 245 core tests passed, and GUI/withdrawn-hook/skill-signature gates passed. See docs/testing/tribe-work-skill-retest.md for live acceptance. Native parity limits and candidate tribe fishing work are documented in the audit; no unsupported duplicate controls were enabled.

Installation, versioning/packaging and higher rare-fish presets are explicitly deferred until the user finishes testing and closes ASKA. The running installation remains 0.1.8. No save or installed plugin files were changed.
