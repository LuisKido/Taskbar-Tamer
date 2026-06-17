using Godot;
using System;

namespace TaskbarTamer.Game;

/// <summary>
/// Opciones del juego: escala de la interfaz (para adaptarse a distintas resoluciones)
/// y "siempre encima". Aplica los cambios en vivo mediante callbacks a <see cref="Main"/>.
/// </summary>
public partial class SettingsPanel : Control
{
    public event Action? Closed;

    private const float Step = 0.25f;
    private const float MinScale = 0.75f;
    private const float MaxScale = 2.5f;

    private float _scale;
    private Action<float>? _onScale;
    private Action<bool>? _onTop;
    private Label _scaleLabel = null!;

    public void Begin(float scale, bool alwaysOnTop, Action<float> onScale, Action<bool> onTop)
    {
        _scale = scale;
        _onScale = onScale;
        _onTop = onTop;
        BuildUi(alwaysOnTop);
    }

    private void BuildUi(bool alwaysOnTop)
    {
        var backdrop = new ColorRect { Color = new Color(0.11f, 0.11f, 0.13f) };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var bg = new PanelContainer();
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 14);
        bg.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        var header = new HBoxContainer();
        root.AddChild(header);
        var title = new Label { Text = "⚙ Opciones" };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        header.AddChild(back);

        root.AddChild(new HSeparator());

        // Escala de UI.
        var scaleRow = new HBoxContainer();
        root.AddChild(scaleRow);
        var scaleTitle = new Label { Text = "Escala de la interfaz" };
        scaleTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scaleRow.AddChild(scaleTitle);

        var minus = new Button { Text = "−", FocusMode = FocusModeEnum.None };
        minus.Pressed += () => ChangeScale(-Step);
        scaleRow.AddChild(minus);

        _scaleLabel = new Label { Text = ScaleText(), HorizontalAlignment = HorizontalAlignment.Center };
        _scaleLabel.CustomMinimumSize = new Vector2(54, 0);
        scaleRow.AddChild(_scaleLabel);

        var plus = new Button { Text = "+", FocusMode = FocusModeEnum.None };
        plus.Pressed += () => ChangeScale(Step);
        scaleRow.AddChild(plus);

        var hint = new Label
        {
            Text = "Sube la escala si el juego se ve pequeño en tu pantalla.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.Modulate = new Color(1, 1, 1, 0.5f);
        hint.AddThemeFontSizeOverride("font_size", 11);
        root.AddChild(hint);

        // Siempre encima.
        var topRow = new HBoxContainer();
        root.AddChild(topRow);
        var topTitle = new Label { Text = "Ventana siempre encima" };
        topTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topRow.AddChild(topTitle);
        var toggle = new CheckButton { ButtonPressed = alwaysOnTop, FocusMode = FocusModeEnum.None };
        toggle.Toggled += on => _onTop?.Invoke(on);
        topRow.AddChild(toggle);
    }

    private void ChangeScale(float delta)
    {
        _scale = Mathf.Clamp(Mathf.Snapped(_scale + delta, 0.05f), MinScale, MaxScale);
        _scaleLabel.Text = ScaleText();
        _onScale?.Invoke(_scale);
    }

    private string ScaleText() => $"{_scale:0.00}x";

    private void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }
}
