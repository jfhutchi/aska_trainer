using BepInEx.Configuration;
using HutchASKA.Core.Input;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Input;

internal sealed class HotkeyManager
{
    public ConfigEntry<KeyCode> Menu { get; }
    private readonly (string Id, ConfigEntry<KeyCode> Key)[] keys;
    private readonly FeatureHost host;
    private readonly SinglePlayerGuard guard;
    private readonly RuntimeConfiguration config;
    public HotkeyManager(ConfigFile file, FeatureHost host, SinglePlayerGuard guard, RuntimeConfiguration config)
    {
        this.host = host;
        this.guard = guard;
        this.config = config;
        Menu = file.Bind("Controls", "MenuHotkey", KeyCode.F8, "Show or hide trainer.");
        keys = new[] {
            ("player.god", file.Bind("Controls", "GodModeHotkey", KeyCode.F1, "Toggle God Mode.")),
            ("player.stamina", file.Bind("Controls", "StaminaHotkey", KeyCode.F2, "Toggle Infinite Stamina.")),
            ("world.freeze", file.Bind("Controls", "FreezeTimeHotkey", KeyCode.F5, "Toggle Freeze Time.")) };
    }
    public void Tick()
    {
        foreach (var binding in keys)
        {
            var key = binding.Key.Value;
            if (key == KeyCode.None || key == Menu.Value || keys.Count(k => k.Key.Value == key) != 1 || !UnityEngine.Input.GetKeyDown(key)) continue;
            if (!guard.Refresh().Allowed) return;
            var feature = host.Registry.Find(binding.Id);
            if (feature is not null && FeatureInputActions.TryToggle(feature)) config.Remember(feature);
        }
    }
}
