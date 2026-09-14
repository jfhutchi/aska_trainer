using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Core.World;

namespace HutchASKA.Plugin.World;

internal sealed class GameSpeedFeature() : NativeFeature("world.speed", "Game Speed")
{
    public MultiplierSetting Multiplier { get; } = new(.5f, 5);
    private readonly GameSpeedController speed = new();
    public override CompatibilityResult ProbeCompatibility()
    {
        var property = typeof(Time).GetProperty("timeScale");
        return property?.PropertyType == typeof(float) && property.CanRead && property.CanWrite
            ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Unity timeScale access is unavailable.");
    }
    public override void Tick()
    {
        if (Multiplier.Value == 1) { Restore(); return; }
        if (speed.Target(Time.timeScale, Multiplier.Value) is { } target)
        {
            Time.timeScale = target;
            speed.RecordApplied(target);
        }
    }
    private void Restore()
    {
        if (speed.RestoreTarget(Time.timeScale) is { } native) Time.timeScale = native;
        speed.Clear();
    }
    public override void Disable() { Restore(); base.Disable(); }
    public override void Reset() { Disable(); Multiplier.Reset(); }
}
