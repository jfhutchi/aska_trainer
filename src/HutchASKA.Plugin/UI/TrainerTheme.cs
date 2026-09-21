extern alias UnityCore;
using NativeObject = UnityCore::UnityEngine.Object;
using UnityEngine;

namespace HutchASKA.Plugin.UI;

internal sealed class TrainerTheme : IDisposable
{
    private readonly List<Texture2D> textures = new();
    private GUISkin? skin;
    private int sourceIdentity;
    public GUISkin Skin => skin ?? throw new InvalidOperationException("Trainer theme is not initialized.");
    public GUIStyle SelectedTab { get; private set; } = null!;

    public void Initialize(GUISkin source)
    {
        var identity = source.GetInstanceID();
        if (skin is not null && skin && sourceIdentity == identity && textures.All(texture => texture)) return;
        Dispose();
        skin = NativeObject.Instantiate(source).Cast<GUISkin>();
        skin.hideFlags = HideFlags.HideAndDontSave;
        sourceIdentity = identity;
        var panel = Solid(new Color(0.055f, 0.075f, 0.10f, 1));
        var control = Solid(new Color(0.17f, 0.22f, 0.28f, 1));
        var hover = Solid(new Color(0.24f, 0.34f, 0.42f, 1));
        var selected = Solid(new Color(0.08f, 0.39f, 0.48f, 1));

        skin.window = Copy(source.window, 18);
        SetBackground(skin.window, panel);
        skin.window.padding = new RectOffset(14, 14, 36, 14);
        skin.window.border = new RectOffset();
        skin.window.alignment = TextAnchor.UpperCenter;
        skin.label = Copy(source.label, 16);
        skin.label.wordWrap = true;
        skin.button = Copy(source.button, 15);
        skin.button.fixedHeight = 34;
        skin.button.padding = new RectOffset(10, 10, 4, 4);
        skin.button.border = new RectOffset();
        SetBackground(skin.button, control);
        skin.button.hover.background = hover;
        skin.button.active.background = selected;
        skin.button.focused.background = hover;
        SelectedTab = new GUIStyle(skin.button);
        SetBackground(SelectedTab, selected);

        skin.toggle = Copy(source.toggle, 16);
        skin.toggle.fixedHeight = 30;
        skin.toggle.padding = new RectOffset(24, 4, 5, 5);
        skin.textField = Copy(source.textField, 16);
        skin.textField.fixedHeight = 34;
        skin.textField.padding = new RectOffset(8, 8, 5, 5);
        skin.textField.border = new RectOffset();
        SetBackground(skin.textField, control);
    }

    private Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        textures.Add(texture);
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }

    private static GUIStyle Copy(GUIStyle source, int fontSize)
    {
        var style = new GUIStyle(source) { fontSize = fontSize };
        foreach (var state in States(style)) state.textColor = Color.white;
        return style;
    }

    private static GUIStyleState[] States(GUIStyle style) => new[]
    {
        style.normal, style.hover, style.active, style.focused,
        style.onNormal, style.onHover, style.onActive, style.onFocused
    };

    private static void SetBackground(GUIStyle style, Texture2D background)
    {
        foreach (var state in States(style)) state.background = background;
    }

    public void Dispose()
    {
        if (skin is not null && skin) NativeObject.Destroy(skin);
        skin = null;
        sourceIdentity = 0;
        foreach (var texture in textures) if (texture) NativeObject.Destroy(texture);
        textures.Clear();
    }
}
