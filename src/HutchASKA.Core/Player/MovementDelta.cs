using System.Numerics;

namespace HutchASKA.Core.Player;

public static class MovementDelta
{
    public static float ScaleNativeSpeed(float nativeSpeed, float multiplier)
    {
        if (!float.IsFinite(nativeSpeed) || nativeSpeed < 0) throw new ArgumentOutOfRangeException(nameof(nativeSpeed));
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 5) throw new ArgumentOutOfRangeException(nameof(multiplier));
        var scaled = nativeSpeed * multiplier;
        if (!float.IsFinite(scaled)) throw new InvalidOperationException("Scaled native speed is not finite.");
        return scaled;
    }

    public static Vector3 ScaleHorizontal(Vector3 before, Vector3 after, float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 5)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        if (!IsFinite(before) || !IsFinite(after))
            throw new ArgumentOutOfRangeException(nameof(after), "Native movement sample is not finite.");
        if (multiplier == 1) return after;
        var result = new Vector3(
            before.X + (after.X - before.X) * multiplier,
            after.Y,
            before.Z + (after.Z - before.Z) * multiplier);
        if (!IsFinite(result)) throw new InvalidOperationException("Scaled movement sample is not finite.");
        return result;
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
