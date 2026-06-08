using Godot;

namespace TaskbarTamer.Game;

/// <summary>
/// Pantalla compacta de Taskbar Tamer: ventana sin bordes y movible. Carga la partida,
/// aplica el progreso de farming offline al abrir y deja farmear bajo demanda. Toda la
/// lógica vive en core/ vía <see cref="GameSession"/>.
/// </summary>
public partial class Main : Control
{
    private readonly GameSession _session = new();
    private bool _dragging;
    private Label _statusLabel = null!;
    private Label _offlineLabel = null!;

    public override void _Ready()
    {
        BuildUi();

        long now = (long)Time.GetUnixTimeFromSystem();
        _session.LoadOrCreate(now);
        _session.ApplyOfflineProgress(now);

        Refresh();
    }

    private void BuildUi()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 5);
        margin.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);

        var title = new Label { Text = "🐾 Taskbar Tamer" };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);

        var close = new Button { Text = "✕", FocusMode = FocusModeEnum.None };
        close.Pressed += () => GetTree().Quit();
        header.AddChild(close);

        vbox.AddChild(new HSeparator());

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        vbox.AddChild(_statusLabel);

        _offlineLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _offlineLabel.Modulate = new Color(0.6f, 0.9f, 0.6f);
        vbox.AddChild(_offlineLabel);

        var farmButton = new Button { Text = "Farmear +1 h", FocusMode = FocusModeEnum.None };
        farmButton.Pressed += OnFarmPressed;
        vbox.AddChild(farmButton);

        var hint = new Label { Text = "Arrastra para mover · cierra y reabre: el progreso se guarda" };
        hint.Modulate = new Color(1, 1, 1, 0.45f);
        hint.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(hint);
    }

    private void OnFarmPressed()
    {
        _session.FarmFor(3600);
        Refresh();
    }

    private void Refresh()
    {
        var state = _session.State;
        _statusLabel.Text =
            $"Equipo: {state.Roster.Count} criatura(s) · poder {_session.TeamPower}\n" +
            $"Bioma: {state.CurrentBiomeId}\n" +
            $"Inventario: {state.Inventory.Count} partes";

        if (_session.LastOfflineLoot > 0)
        {
            long mins = _session.LastOfflineSeconds / 60;
            _offlineLabel.Text = $"Mientras no estabas ({mins} min): +{_session.LastOfflineLoot} botín";
            _offlineLabel.Visible = true;
        }
        else
        {
            _offlineLabel.Visible = false;
        }
    }

    public override void _ExitTree()
    {
        _session.Save();
    }

    // Arrastre de la ventana sin bordes: mover con el botón izquierdo pulsado.
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            GetWindow().Position += new Vector2I((int)mm.Relative.X, (int)mm.Relative.Y);
        }
    }
}
