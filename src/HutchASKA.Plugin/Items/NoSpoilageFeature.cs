using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Items;

internal sealed class NoSpoilageFeature() : NativeFeature("items.freshness", "No Spoilage")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Expiration Run(Item, ref float deltaTime) exists; freshness-only interception has not been verified.");
}
