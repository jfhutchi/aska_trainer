using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Items;

internal sealed class NoSpoilageFeature() : NativeFeature("items.freshness", "No Spoilage (carried items)")
{
    private const string UnavailableReason =
        "Temporarily unavailable: the item decay hook caused repeated game errors. Normal spoilage remains active.";

    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(UnavailableReason);

    public override bool TryEnable()
    {
        StatusReason = UnavailableReason;
        return false;
    }
}
