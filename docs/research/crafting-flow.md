# Crafting transaction inspection

Current Assembly-CSharp build 25186770 exposes these instance methods on SSSGame.CraftInteraction:

- `bool CheckOwnedRequirements(Blueprint,IInteractionAgent)`
- `bool _CheckOwnedBlueprintManifest(SandSailorStudio.Inventory.ItemManifest,IInteractionAgent)`
- `void BeginCraftingSequence(InteractionSession)`
- `void _OnCraftingSuccess(IInteractionAgent)`
- `void Finish(IInteractionAgent,InteractionSessionState)`

The checks are not parameterless. Blueprint exposes FillPartsManifest, GetResultInfo and GetResultQuantity. SandSailorStudio ItemCollection.RemoveOwnedItemManifest is shared across consumers.

Generated native-invoke wrappers do not prove ordering, which checks include unlocks, or an isolated crafting-only consumption scope. These are candidate entry points, not a traced complete transaction. Returning true from a combined requirement check could accidentally bypass blueprints; global manifest removal suppression would affect other actions. Free crafting is therefore Incompatible and installs no patch. Optional blueprint bypass is omitted. Zero-material native product creation, disable behavior and save persistence remain MANUAL VERIFICATION REQUIRED after a narrow transaction is proven.
