using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.World;

internal sealed class MushroomRegrowthFeature() : NativeFeature("world.mushrooms", "Mushroom Regrowth")
{
    private const string UnavailableReason =
        "Temporarily unavailable: accelerated mushroom timers can persist through autosave. Normal regrowth remains active.";

    public MultiplierSetting Multiplier { get; } = new(1, 4);

    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(UnavailableReason);

    public override bool TryEnable()
    {
        Multiplier.Reset();
        StatusReason = UnavailableReason;
        return false;
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
