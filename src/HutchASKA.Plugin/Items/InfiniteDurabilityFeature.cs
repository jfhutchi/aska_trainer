using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Items;

internal sealed class InfiniteDurabilityFeature() : NativeFeature("items.durability", "Infinite Durability (carried equipment)")
{
    private const string UnavailableReason =
        "Temporarily unavailable: item decay and tool-wear hooks caused repeated game errors. Normal durability remains active.";

    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(UnavailableReason);

    public override bool TryEnable()
    {
        StatusReason = UnavailableReason;
        return false;
    }
}
