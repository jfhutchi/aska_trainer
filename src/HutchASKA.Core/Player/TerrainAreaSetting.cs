namespace HutchASKA.Core.Player;

public sealed class TerrainAreaSetting
{
    private int value = 5;
    public int Value { get => value; set => this.value = Math.Clamp(value, 5, 20); }
    public void Reset() => value = 5;

    // Native network storage packs 85 three-bit cells into each BitSet256.
    public static int MaximumSide(int requested, int networkElements)
    {
        if (networkElements is not (3 or 6)) return 5;
        return Math.Min(Math.Clamp(requested, 5, 20), (int)Math.Sqrt(networkElements * 85));
    }

    public static bool Fits(int width, int depth, int maximumSide) =>
        width >= -maximumSide && width <= maximumSide && depth >= -maximumSide && depth <= maximumSide;
}
