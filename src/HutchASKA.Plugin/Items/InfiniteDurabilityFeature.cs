using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Attributes;
using SandSailorStudio.Inventory;
using SSSGame;
using NativeAttribute = SandSailorStudio.Attributes.Attribute;

namespace HutchASKA.Plugin.Items;

internal sealed class InfiniteDurabilityFeature(IPlayerContext players) : NativeFeature("items.durability", "Infinite Durability (player equipment)")
{
    private const int DecayPropertyId = 1010;
    private const long RefreshIntervalMs = 250;
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".durability");
    private readonly HashSet<long> currentProperties = new();
    private readonly HashSet<long> nextProperties = new();
    private static InfiniteDurabilityFeature? active;
    private long nextRefreshAt;
    private int prevented;
    private int reportedCount = -1;
    private int reportedPrevented = -1;

    public override CompatibilityResult ProbeCompatibility() =>
        new[] { typeof(Property), typeof(NativeAttribute), typeof(VariableAttribute) }
            .All(type => AccessTools.DeclaredMethod(type, "SetValue", new[] { typeof(float) })?.ReturnType == typeof(void))
        && typeof(EquipmentItem).GetMethod("TryGetProperty", new[] { typeof(int).MakeByRefType(), typeof(Property).MakeByRefType() })?.ReturnType == typeof(bool)
        && typeof(ItemCollection).GetMethods().Any(method => method.Name == "GetAllItems" && !method.IsGenericMethod
            && method.GetParameters().Length == 0)
        && typeof(EquipPoint).GetMethod("GetCurrentItem", Type.EmptyTypes)?.ReturnType == typeof(EquipmentItem)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Current equipment durability property or player inventory APIs are unavailable.");

    public override bool TryEnable()
    {
        foreach (var type in new[] { typeof(Property), typeof(NativeAttribute), typeof(VariableAttribute) })
            harmony.Patch(AccessTools.DeclaredMethod(type, "SetValue", new[] { typeof(float) }),
                prefix: new HarmonyMethod(typeof(InfiniteDurabilityFeature), nameof(SetValuePrefix)));
        active = this;
        nextRefreshAt = 0;
        prevented = 0;
        RefreshProperties();
        PublishStatus();
        return base.TryEnable();
    }

    public override void Tick()
    {
        if (Environment.TickCount64 >= nextRefreshAt) RefreshProperties();
        if (currentProperties.Count != reportedCount || prevented != reportedPrevented) PublishStatus();
    }

    private void RefreshProperties()
    {
        nextRefreshAt = Environment.TickCount64 + RefreshIntervalMs;
        nextProperties.Clear();
        if (players.TryGetLocalPlayer(out var player) && player!.Inventory is { initialized: true } inventory)
        {
            var collection = inventory.GetItemCollection();
            if (collection is { HasStateOwnership: true })
            {
                var items = collection.GetAllItems();
                for (var i = 0; i < items.Count; i++) AddOwnedEquipment(items[i]?.TryCast<EquipmentItem>());
                var equipment = inventory.TryCast<CharacterInventory>()?._equipmentManager;
                if (equipment is not null && equipment)
                {
                    var points = equipment.equipPoints;
                    for (var i = 0; i < points.Count; i++) AddOwnedEquipment(points[i]?.GetCurrentItem());
                }
            }
        }
        currentProperties.Clear();
        currentProperties.UnionWith(nextProperties);
    }

    private void AddOwnedEquipment(EquipmentItem? item)
    {
        if (item is null || !LocalItemOwnership.IsCarriedByLocalPlayer(players, item)) return;
        var id = DecayPropertyId;
        if (item.TryGetProperty(ref id, out var property) && property is not null && property.id == DecayPropertyId)
            nextProperties.Add(property.Pointer.ToInt64());
    }

    private bool StillOwnsProperty(long pointer)
    {
        if (!players.TryGetLocalPlayer(out var player) || player!.Inventory is not { initialized: true } inventory)
            return false;
        var collection = inventory.GetItemCollection();
        if (collection is not { HasStateOwnership: true }) return false;
        var items = collection.GetAllItems();
        for (var i = 0; i < items.Count; i++)
            if (MatchesOwnedEquipment(items[i]?.TryCast<EquipmentItem>(), pointer)) return true;
        var equipment = inventory.TryCast<CharacterInventory>()?._equipmentManager;
        if (equipment is null || !equipment) return false;
        var points = equipment.equipPoints;
        for (var i = 0; i < points.Count; i++)
            if (MatchesOwnedEquipment(points[i]?.GetCurrentItem(), pointer)) return true;
        return false;
    }

    private bool MatchesOwnedEquipment(EquipmentItem? item, long pointer)
    {
        if (item is null || !LocalItemOwnership.IsCarriedByLocalPlayer(players, item)) return false;
        var id = DecayPropertyId;
        return item.TryGetProperty(ref id, out var property) && property is not null
            && property.Pointer.ToInt64() == pointer;
    }

    private static bool SetValuePrefix(Property __instance, float __0)
    {
        var feature = active;
        if (feature is null || __instance is null || __instance.id != DecayPropertyId
            || !float.IsFinite(__0) || !feature.currentProperties.Contains(__instance.Pointer.ToInt64())) return true;
        var allow = true;
        feature.Hosted?.TryExecute(() =>
        {
            if (__0 <= __instance.GetValue() || !feature.StillOwnsProperty(__instance.Pointer.ToInt64())) return;
            feature.prevented = Math.Min(feature.prevented + 1, 999999);
            allow = false;
        });
        return allow;
    }

    private void PublishStatus()
    {
        reportedCount = currentProperties.Count;
        reportedPrevented = prevented;
        StatusReason = $"Player equipment tracked: {reportedCount}; wear increases prevented: {reportedPrevented}. Existing damage stays; repairs remain available.";
    }

    public override void Disable()
    {
        active = null;
        harmony.UnpatchSelf();
        currentProperties.Clear();
        nextProperties.Clear();
        nextRefreshAt = 0;
        StatusReason = null;
        base.Disable();
    }
}
