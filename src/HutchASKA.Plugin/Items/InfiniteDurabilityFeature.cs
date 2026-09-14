using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Items;

internal sealed class InfiniteDurabilityFeature() : NativeFeature("items.durability", "Infinite Durability")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Run(Item, ref float deltaTime) is present, but its native loss and break processing are unverified.");
}
