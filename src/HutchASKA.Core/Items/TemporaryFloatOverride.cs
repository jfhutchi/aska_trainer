namespace HutchASKA.Core.Items;

/// <summary>Restores a shared by-reference native input after one intercepted call.</summary>
public readonly record struct TemporaryFloatOverride(bool Applied, float Original)
{
    public static TemporaryFloatOverride ZeroPositive(ref float value)
    {
        if (!float.IsFinite(value) || value <= 0) return default;
        var state = new TemporaryFloatOverride(true, value);
        value = 0;
        return state;
    }

    public void Restore(ref float value)
    {
        if (Applied) value = Original;
    }
}
