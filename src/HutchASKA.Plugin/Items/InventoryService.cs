using System.Globalization;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;

namespace HutchASKA.Plugin.Items;

internal sealed class InventoryService(IPlayerContext players, SinglePlayerGuard guard)
{
    public const int MaximumQuantity = 999;
    public bool TryGive(string itemId, int quantity, out string? error) => Give(itemId, quantity, false, out error);
    public bool TryGiveStack(string itemId, out string? error) => Give(itemId, 1, true, out error);

    private bool Give(string itemId, int quantity, bool stack, out string? error)
    {
        error = null;
        var decision = guard.Refresh();
        if (!decision.Allowed) { error = decision.Reason; return false; }
        if (!int.TryParse(itemId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        { error = "Unknown item ID."; return false; }
        var database = GameObjectResolver.FindUnique<ItemInfoDatabase>();
        if (!database) { error = "Item definitions are not loaded."; return false; }
        var info = database!.GetItemInfoFromID(id);
        if (!info) { error = "This item no longer exists in the current catalog."; return false; }
        if (stack) quantity = info.stackSize;
        if (quantity < 1 || quantity > MaximumQuantity)
        { error = $"Quantity must be between 1 and {MaximumQuantity}."; return false; }
        if (!players.TryGetLocalPlayer(out var player)) { error = "Local player is unavailable."; return false; }
        var inventory = player!.Inventory;
        if (!inventory || !inventory.initialized) { error = "Player inventory is not initialized."; return false; }
        var collection = inventory.GetItemCollection();
        if (collection is null || !collection.canAddItems || !collection.HasStateOwnership)
        { error = "Local inventory cannot currently receive items."; return false; }
        if (collection.GetTotalRemainingCapacity(info) < quantity)
        { error = "Not enough inventory space for this quantity."; return false; }
        var before = collection.GetItemQuantity(info);
        decision = guard.Refresh();
        if (!decision.Allowed) { error = decision.Reason; return false; }
        // Native definition overload owns item initialization, container selection and events.
        collection.AddItems(info, quantity);
        var added = collection.GetItemQuantity(info) - before;
        if (added != quantity)
        { error = $"Native inventory reports {added} of {quantity} added. Check inventory before retrying."; return false; }
        return true;
    }
}
