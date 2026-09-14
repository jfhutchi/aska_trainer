using BepInEx.Logging;
using HutchASKA.Core.Features;

namespace HutchASKA.Plugin.Infrastructure;

public sealed class FeatureHost(SinglePlayerGuard guard, ManualLogSource log)
{
    public FeatureRegistry Registry { get; } = new();

    public HostedFeature Register(ITrainerFeature feature)
    {
        var hosted = new HostedFeature(feature, guard.Refresh,
            (operation, error) => log.LogError($"{operation}: {error}"));
        Registry.Register(hosted);
        var compatibility = hosted.ProbeCompatibility();
        if (!compatibility.IsCompatible)
            log.LogWarning($"{hosted.Id} incompatible: {compatibility.Reason}");
        return hosted;
    }

    public void Tick()
    {
        guard.Refresh();
        foreach (var feature in Registry.Snapshot()) feature.Tick();
    }

    public void DisableAll()
    {
        foreach (var feature in Registry.Snapshot()) feature.Disable();
    }
}
