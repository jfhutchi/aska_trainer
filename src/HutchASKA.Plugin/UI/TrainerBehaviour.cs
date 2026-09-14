using BepInEx.Configuration;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace HutchASKA.Plugin.UI;

public sealed class TrainerBehaviour(IntPtr pointer) : MonoBehaviour(pointer)
{
    private FeatureHost? host;
    private TrainerWindow? window;
    private ConfigEntry<KeyCode>? menuKey;
    private bool visible;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    [HideFromIl2Cpp]
    public void Initialize(FeatureHost featureHost, TrainerWindow trainerWindow, ConfigEntry<KeyCode> hotkey)
    {
        DontDestroyOnLoad(gameObject);
        host = featureHost;
        window = trainerWindow;
        menuKey = hotkey;
    }

    public void Update()
    {
        if (menuKey is not null && Input.GetKeyDown(menuKey.Value)) SetVisible(!visible);
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
        }
        else
        {
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }
}
