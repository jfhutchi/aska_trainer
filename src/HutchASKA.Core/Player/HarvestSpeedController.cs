namespace HutchASKA.Core.Player;

/// <summary>Preserves an animator's native baseline while one harvest owns its speed.</summary>
public sealed class HarvestSpeedController
{
    public long? Owner { get; private set; }
    public float? Baseline { get; private set; }
    public float? Applied { get; private set; }

    public float Target(long owner, float nativeSpeed, float multiplier)
    {
        ValidateMultiplier(multiplier);
        if (!float.IsFinite(nativeSpeed) || nativeSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(nativeSpeed));
        if (Owner.HasValue && Owner != owner)
            throw new InvalidOperationException("Release the previous harvest animator before changing targets.");
        Owner = owner;
        if (!Baseline.HasValue || nativeSpeed != Applied) Baseline = nativeSpeed;
        return Baseline.Value * multiplier;
    }

    public void RecordApplied(float speed) => Applied = speed;
    public float? RestoreTarget(long owner, float nativeSpeed) =>
        Owner == owner && nativeSpeed == Applied ? Baseline : null;
    public void Clear() { Owner = null; Baseline = null; Applied = null; }

    public static float GatherThreshold(float nativeThreshold, float multiplier)
    {
        ValidateMultiplier(multiplier);
        if (!float.IsFinite(nativeThreshold) || nativeThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(nativeThreshold));
        return nativeThreshold / multiplier;
    }

    public static float RestoreGatherProgress(float progress, float appliedThreshold, float nativeThreshold)
    {
        if (!float.IsFinite(progress) || progress < 0 || !float.IsFinite(appliedThreshold)
            || appliedThreshold <= 0 || !float.IsFinite(nativeThreshold) || nativeThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(progress));
        return progress / appliedThreshold * nativeThreshold;
    }

    private static void ValidateMultiplier(float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 4)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
    }
}
