namespace HutchASKA.Core.World;

public sealed class GameSpeedController
{
    public float? Baseline { get; private set; }
    public float? Applied { get; private set; }
    public float? Target(float native, float multiplier)
    {
        if (!float.IsFinite(native) || native < 0) throw new ArgumentOutOfRangeException(nameof(native));
        if (native == 0) return null;
        if (!Baseline.HasValue || native != Applied) Baseline = native;
        return Baseline.Value * multiplier;
    }
    public void RecordApplied(float value) => Applied = value;
    public float? RestoreTarget(float native) => native == Applied ? Baseline : null;
    public void Clear() { Baseline = null; Applied = null; }
}
