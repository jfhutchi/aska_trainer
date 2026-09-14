namespace HutchASKA.Core.Features;

public sealed class MultiplierSetting
{
    private readonly float min;
    private readonly float max;
    private float value = 1;
    public MultiplierSetting(float min, float max)
    {
        if (!float.IsFinite(min) || !float.IsFinite(max) || min > 1 || max < 1 || min <= 0)
            throw new ArgumentOutOfRangeException(nameof(min));
        this.min = min;
        this.max = max;
    }
    public float Value { get => value; set => this.value = float.IsFinite(value) ? Math.Clamp(value, min, max) : 1; }
    public void Reset() => value = 1;
}
