using HutchASKA.Plugin.Game;
using SandSailorStudio.Inventory;

namespace HutchASKA.Plugin.Items;

internal static class LocalItemOwnership
{
    internal static bool IsCarriedByLocalPlayer(IPlayerContext players, Item? item)
    {
        if (item is null || item.count <= 0 || !item.HasAuthority()
            || !players.TryGetLocalPlayer(out var player)) return false;
        var inventory = player!.Inventory;
        if (!inventory || !inventory.initialized) return false;
        var local = inventory.GetItemCollection();
        var actual = item.Container?.Inventory;
        if (local is null || !local.HasStateOwnership) return false;
        if (actual is not null && actual.HasStateOwnership && local.Pointer == actual.Pointer) return true;
        var equipment = item.TryCast<EquipmentItem>();
        if (equipment is null || (!equipment.IsEquipped() && !equipment.IsStowed())) return false;
        var actualEquipment = equipment.GetEquipmentManager();
        var localEquipment = inventory.TryCast<SSSGame.CharacterInventory>()?._equipmentManager;
        return actualEquipment && localEquipment && actualEquipment == localEquipment;
    }
}
