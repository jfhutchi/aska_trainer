# Item use and retention inspection

Inspected current build 25186770 generated assemblies on 2026-09-14. Assembly-CSharp exposes `bool SSSGame.Consumable.Use(UnityEngine.GameObject)` and `void _ApplyEffect(StatusEffectManager)`. SandSailorStudio exposes `bool Inventory.Item.Use(GameObject)`, `int Item.Remove(int)`, `int ItemCollection.RemoveItems(Item,int)`, and `bool ItemContainer.RemoveItem(Item,int,ItemEventContext)`.

These are candidate relationships, not a proven native call chain: generated bodies invoke native code. ItemEventContext has Default and SameInventoryTransfer only; neither identifies consumption versus crafting or building. A synchronous consumable-use scope is a possible future investigation, but effect ordering and destruction/detachment of the last stack are unverified. Restoring quantity after removal could leave a detached item.

The requested behavior is unchanged stack quantity with normal use effects. The registered feature is Incompatible and installs no shared removal patch. Food/drink plus another usable item, last-item consumption, normal effects, and independent crafting behavior remain MANUAL VERIFICATION REQUIRED after a narrow hook is established.
