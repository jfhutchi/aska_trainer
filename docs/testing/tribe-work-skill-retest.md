# Pending tribe work and skill gain acceptance

Target: ASKA Steam build 25326768 with the current local BepInEx installation.
These are manual acceptance steps, not completed gameplay results. Keep the
current 0.1.8 game session running until the user finishes their existing test;
source edits do not update a loaded plugin.

## Verification completed before installation

The integrated local-interop build passed with zero warnings/errors on 2026-09-21
after the harvest dealer-identity correction and movement status throttling.
All 245 core tests passed (220 previous, 15 skill-gain, 10 tribe-work cases).
The 78-method GUI gate, withdrawn hook checks and new native by-value skill-award
binding checks passed. Independent source/native reviews found no remaining
actionable code defects. This evidence is not a live gameplay/performance pass.
No installed game files or saves were modified by this pending update.

## Skill experience

Player and tribe gain have independent normal/2x/3x/4x/5x controls. Compare the
same repeatable action below its native proficiency cap at normal and 5x. Verify
the relevant experience remainder or skill progress, not damage or animation
speed. Check player-only, tribe-only, and both enabled; one award must not receive
both multipliers. Include work and fishing/combat examples. Existing native
bonuses, level-cost curves, cap checks and level-up notifications still run.

Return to normal, reload configuration and Reset All. New actions should resume
normal gains. Already earned experience is normal progress and is not removed.
Check a newly joined villager and a departed/dead/guest villager: only current
living owned members qualify. No individual selection or global population scan
is needed to turn the tribe setting on.

## Tribe building and harvesting

Use a fresh build with materials present, Free Building off, and one assigned
villager. Compare normal, 2x and 5x work per full hammer stroke. Verify layer and
structure completion, ordinary material supply, normal player work while only
the tribe control is enabled, and normal tribe work after returning to 1x.
Walking and hammer animation speed are not the measure of work contribution.

For tribe harvesting, compare the same tool/resource at normal and 5x. Verify
fewer work actions to finish eligible tool harvesting and shorter hand-gathering
work, with normal item quantities and completion. Check unrelated combat, travel
and other duties retain normal behavior. Cancel/reassign tasks, destroy a target,
change worlds and reset settings to check that temporary callback state is not
retained. Skills and work multipliers are independent controls.

## Existing pending UI changes

Tribe Movement Speed needs a separate normal/2x/5x walking comparison. Confirm
the boost affects owned members only, preserves stationary behavior and native
pathfinding, and does not speed swimming, vehicles, ladders or special traversal.
Returning to normal should restore the normal target speed through native
acceleration; already covered distance is not reversed.

The trainer should fill the game-window height with 20-pixel top/bottom margins.
Close must remain visible and clickable. Test a resolution change and a long
Tribe/Items page; overflow still scrolls. Player Harvesting Speed appears in
Fishing & Foraging with the same stored value and 1x/2x/3x/4x options.

Build prompt localization should no longer display `(Clone)` keys after a boosted
player or tribe work event. Normal building work already advanced in the user's
0.1.8 log, but the player reported no clear visible speed change; the logged work
increments (~1.87 at 2x, ~3.74 at 4x) do not replace a controlled 1x comparison.

## Other tribe equivalents

Existing tribe protection, needs and warmth controls remain available. Free
Building and Free Repairs operate on eligible shared settlement structures.
World controls retain their world scope. See the parity audit for player-only
transactions and withdrawn features that cannot be represented by a working
tribe checkbox without further native verification.

The rare-fish 20x/30x/40x/50x presets are separately requested for the installation
window after the user closes ASKA; they are not part of this source change yet.
