using BepInEx.Configuration;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Input;
using HutchASKA.Core.Features;

namespace HutchASKA.Plugin.UI;

public sealed class TrainerBehaviour(IntPtr pointer) : MonoBehaviour(pointer)
{
    private FeatureHost? host;
    private TrainerWindow? window;
    private ConfigEntry<KeyCode>? menuKey;
    private HotkeyManager? hotkeys;
    private RuntimeConfiguration? config;
    private SinglePlayerGuard? guard;
    private HostedFeature? menuInput;
    private bool visible;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    [HideFromIl2Cpp]
    internal void Initialize(FeatureHost featureHost, TrainerWindow trainerWindow, HotkeyManager hotkeys,
        RuntimeConfiguration config, SinglePlayerGuard guard, HostedFeature menuInput)
    {
        DontDestroyOnLoad(gameObject);
        host = featureHost;
        window = trainerWindow;
        this.hotkeys = hotkeys;
        this.config = config;
        this.guard = guard;
        this.menuInput = menuInput;
        menuKey = hotkeys.Menu;
    }

    public void Update()
    {
        if (menuKey is not null && UnityEngine.Input.GetKeyDown(menuKey.Value)) SetVisible(!visible);
        if (guard is not null) config?.TryRestore(guard);
        hotkeys?.Tick();
        host?.Tick();
    }

    public void LateUpdate()
    {
        if (!visible) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnGUI()
    {
        if (visible && window is not null && !window.Draw()) SetVisible(false);
    }

    public void OnDisable()
    {
        SetVisible(false);
        host?.DisableAll();
    }

    private void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        if (visible)
        {
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            menuInput?.TryEnable();
        }
        else
        {
            menuInput?.Disable();
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }
}
