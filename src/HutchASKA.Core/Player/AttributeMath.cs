namespace HutchASKA.Core.Player;

public static class AttributeMath
{
    public static float ValueAtFraction(float min, float max, float fraction)
    {
        if (!float.IsFinite(min) || !float.IsFinite(max) || max < min || !float.IsFinite(fraction))
            throw new ArgumentOutOfRangeException(nameof(fraction), "Attribute range and fraction must be finite and ordered.");
        return (float)(min + ((double)max - min) * Math.Clamp(fraction, 0, 1));
    }
}
