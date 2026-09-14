using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.World;

internal sealed class GameSpeedFeature() : NativeFeature("world.speed", "Game Speed")
{
    public MultiplierSetting Multiplier { get; } = new(.5f, 5);
    private float? previousScale;
    public override CompatibilityResult ProbeCompatibility()
    {
        var property = typeof(Time).GetProperty("timeScale");
        return property?.PropertyType == typeof(float) && property.CanRead && property.CanWrite
            ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Unity timeScale access is unavailable.");
    }
    public override void Tick()
    {
        if (Multiplier.Value == 1) { Restore(); return; }
        if (!previousScale.HasValue)
        {
            var scale = Time.timeScale;
            if (!float.IsFinite(scale) || scale < 0) throw new InvalidOperationException("Invalid native Unity time scale.");
            previousScale = scale;
        }
        Time.timeScale = previousScale.Value * Multiplier.Value;
    }
    private void Restore()
    {
        if (!previousScale.HasValue) return;
        var native = previousScale.Value;
        previousScale = null;
        Time.timeScale = native;
    }
    public override void Disable() { Restore(); base.Disable(); }
    public override void Reset() { Disable(); Multiplier.Reset(); }
}
