namespace HutchASKA.Core.Player;

public static class BuildWorkAmount
{
    public static float Scale(float nativeAmount, float multiplier)
    {
        if (!float.IsFinite(nativeAmount)) throw new ArgumentOutOfRangeException(nameof(nativeAmount));
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 4)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        if (nativeAmount <= 0 || multiplier == 1) return nativeAmount;
        var scaled = nativeAmount * multiplier;
        if (!float.IsFinite(scaled)) throw new InvalidOperationException("Scaled building work is not finite.");
        return scaled;
    }
}
