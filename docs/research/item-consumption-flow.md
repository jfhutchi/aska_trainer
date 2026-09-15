# Consumable use and retention native evidence

Inspected ASKA Steam build 25186770, Unity 6000.3.12f1, IL2CPP metadata 39 on
2026-09-14. Generated signatures were checked with Mono.Cecil; LibCpp2IL and
Capstone established the native chain from the installed game. No game/save
writes or live consumption tests were performed. Addresses below are evidence
locations, not runtime patch offsets.

## Normal use sequence

`SSSGame.Consumable.Use(GameObject)` at 0x180D80F70:

1. Resolves the supplied user object, or `Container.Inventory.Owner` when the
   argument is null. Those field identities were checked against metadata and
   current generated properties.
2. Invokes the normal native consumption receiver and consume animation.
3. Resolves the user's `StatusEffectManager` and calls this consumable's
   `_ApplyEffect`. That function applies the configured effects and conditional
   property-dependent effects through the normal status-effect manager.
4. Resolves this consumable's current container and calls
   `ItemContainer.RemoveItem(this, 1, ItemEventContext.Default)` at 0x180D81115.
   It then returns true. The removal result is not used for the earlier effects.

`ItemContainer.RemoveItem(Item, int, ItemEventContext)` at 0x18363D8D0 first
decrements the item through its native item method. At zero it removes the item
from the container's list and unregisters it from item processing. It sends
normal removal/count events, then detaches a zero-count item. Thus incrementing
count after removal is unsafe for the last stack.

## Implemented scope

`Retain Consumables On Use` establishes a thread-local scope for one carried
consumable whose authority/current container belong to the live local player.
The native supplied-or-fallback user object must also equal the current player's
game object. Passing a foreign consumer does not qualify.

The scope is armed only after that exact consumable's native `_ApplyEffect`
returns. Until then all removals remain native. The subsequent removal hook
requires the same item and container identities, quantity exactly one, Default
context, and freshly rechecked local ownership. It suppresses one such decrement
before count changes, returns the normal successful removal result, and consumes
its authorization. There is no zero-stack transition, reattachment, new-item
construction, refill, duplicate grant, or artificial removal event.

Native effects and animation still run. Nested uses have separate scopes and
restore their outer scope with an exception-preserving finalizer. Ordinary
`Item.Use`, `Item.Remove`, `ItemCollection.RemoveItems`, and `RemoveAllItems` are
not patched. The `ItemContainer.RemoveItem` hook does nothing without a completed
matching consumable-effects scope, so ordinary crafting, building, trading,
discarding, and inventory transfers retain their native spending behavior.

The feature deliberately does not retain a consumable if its native
`_ApplyEffect` callback is absent or fails, if ownership changes during use, or if
the operation differs from the verified one-item Default-context removal.
Other usable item classes are not supported by this hook. Disabling unpatches
the feature's own Harmony ID and holds no item wrappers across frames.

## Verification limits

Core scope tests verify effects-before-authorization, exact item/container
matching, quantity/context exclusion, one-use consumption, missing identities,
and independent nested scopes. Compilation checks the interop methods. Neither
establishes successful live IL2CPP dispatch or food/drink effects.

Manual acceptance remains: food/drink and another actual Consumable subtype,
partly spoiled and fresh items, stacks of one and several, normal nutrition and
status effects, repeated use, foreign consumers, storage/drop/transfer during
use, crafting and building ingredients, independent toggles, disable/reset, and
save/reload. Confirm the original final item remains attached and usable and
that ordinary spending resumes when disabled. No gameplay success is claimed.
