namespace HutchASKA.Core.Configuration;

public sealed class TrainerSettings
{
    public bool RestoreEnabledStatesOnLaunch { get; set; }
    public float MovementSpeedMultiplier { get; set; } = 1f;
    public float GameSpeedMultiplier { get; set; } = 1f;
    public string MenuHotkey { get; set; } = "F8";
    public static TrainerSettings CreateDefaults() => new();
    public static float ClampMovementMultiplier(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, 1f, 5f) : 1f;
}
