using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;

namespace HutchASKA.Plugin.Items;

internal sealed class GiveItemFeature(InventoryService inventory) : NativeActionFeature("items.give", "Give Item / Give Stack")
{
    public override CompatibilityResult ProbeCompatibility() =>
        typeof(ItemCollection).GetMethod("AddItems", new[] { typeof(ItemInfo), typeof(int) })?.ReturnType == typeof(int)
        && typeof(ItemCollection).GetMethod("GetTotalRemainingCapacity", new[] { typeof(ItemInfo) }) is not null
        && typeof(ItemCollection).GetMethod("GetItemQuantity", new[] { typeof(ItemInfo) }) is not null
        ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Native definition-based insertion is unavailable.");

    public bool TryGive(string id, int quantity, bool stack, out string? error)
    {
        string? failure = null;
        var inserted = false;
        var ran = RunOnce(() => inserted = stack ? inventory.TryGiveStack(id, out failure)
            : inventory.TryGive(id, quantity, out failure), out error);
        error ??= failure;
        if (!ran) error += " If insertion had begun, inspect inventory before retrying.";
        return ran && inserted;
    }
}
