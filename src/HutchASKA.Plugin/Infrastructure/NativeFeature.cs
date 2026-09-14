using HutchASKA.Core.Features;

namespace HutchASKA.Plugin.Infrastructure;

internal abstract class NativeFeature(string id, string displayName) : ITrainerFeature
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public FeatureState State { get; private set; }
    public string? StatusReason { get; protected set; }
    public HostedFeature? Hosted { get; set; }
    public abstract CompatibilityResult ProbeCompatibility();
    public virtual bool TryEnable() { State = FeatureState.Enabled; return true; }
    public virtual void Disable() { State = FeatureState.Disabled; }
    public virtual void Reset() => Disable();
    public virtual void Tick() { }
}
