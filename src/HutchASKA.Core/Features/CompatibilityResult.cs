namespace HutchASKA.Core.Features;

public sealed record CompatibilityResult(bool IsCompatible, string? Reason)
{
    public static CompatibilityResult Compatible() => new(true, null);
    public static CompatibilityResult Incompatible(string reason) => new(false, reason);
}
