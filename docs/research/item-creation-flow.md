# Native item creation and insertion

Build 25186770 metadata inspected 2026-09-14. `SSSGame.Character.Inventory` returns `SandSailorStudio.Inventory.InventoryComponent`. Its `initialized` flag and `GetItemCollection()` expose the local character's initialized collection. `ItemInfoDatabase.GetItemInfoFromID(int)` resolves the live definition.

Verified SandSailorStudio signatures:

- `Item ItemInfo.CreateItem()`
- `int ItemCollection.AddItems(ItemInfo,int)` and separate `(Item,int)` overload
- `int GetTotalRemainingCapacity(ItemInfo)`
- `int GetItemQuantity(ItemInfo)`
- `bool canAddItems` and `bool HasStateOwnership`

Give uses the native definition-based AddItems overload, allowing ASKA to initialize items, choose containers and apply native stack rules. No raw Item constructor, static global GiveItem helper, or manually edited quantity is used. Capacity is checked first, positive quantities are limited to 999, and Give Stack reads the current definition's stackSize. The return integer's semantics are not assumed: before/after native quantity is measured. Partial insertion is surfaced explicitly without a second insertion or speculative rollback.

Generated wrappers prove signatures, not full native factory internals or save behavior. This implementation is pending gameplay acceptance. Give a normal stack and a partially occupied inventory, inspect initialized durability/freshness and native stack behavior, then save/exit/reload. Repeat with full inventory and unknown IDs. **MANUAL VERIFICATION REQUIRED**; save persistence is not yet proven and the build is not release-ready.
