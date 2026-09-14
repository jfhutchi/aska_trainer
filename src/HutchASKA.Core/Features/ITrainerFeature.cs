namespace HutchASKA.Core.Features;

public interface ITrainerFeature
{
    string Id { get; }
    string DisplayName { get; }
    FeatureState State { get; }
    string? StatusReason { get; }
    CompatibilityResult ProbeCompatibility();
    bool TryEnable();
    void Disable();
    void Reset();
    void Tick();
}
