using Godot;
using System;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Data;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Pantalla compacta de Taskbar Tamer: ventana sin bordes y movible. Carga la partida,
/// aplica el progreso de farming offline al abrir y deja farmear bajo demanda. Toda la
/// lógica vive en core/ vía <see cref="GameSession"/>.
/// </summary>
public partial class Main : Control
{
    private static readonly Vector2I CompactSize = new(420, 540);
    private static readonly Vector2I BattleSize = new(560, 400);
    private static readonly Vector2I ManageSize = new(680, 540);
    private static readonly Vector2I RecruitSize = new(380, 420);
    private static readonly Vector2I FormationSize = new(460, 440);
    private static readonly Vector2I SettingsSize = new(360, 250);

    private float _uiScale = 1f;

    private readonly GameSession _session = new();
    private bool _dragging;
    private Vector2I _dragMouseStart;   // posición del ratón en pantalla al empezar a arrastrar
    private Vector2I _dragWindowStart;  // posición de la ventana al empezar a arrastrar
    private Label _statusLabel = null!;
    private Label _offlineLabel = null!;
    private ArenaView _arena = null!;
    private PackedScene _battleScene = null!;
    private PackedScene _manageScene = null!;
    private PackedScene _recruitScene = null!;
    private PackedScene _formationScene = null!;
    private PanelContainer _mainPanel = null!;
    private Battle? _activeBattle;
    private ManagementPanel? _activeManage;
    private RecruitPanel? _activeRecruit;
    private FormationPanel? _activeFormation;
    private PackedScene _settingsScene = null!;
    private SettingsPanel? _activeSettings;

    public override void _Ready()
    {
        _battleScene = GD.Load<PackedScene>("res://Scenes/Battle.tscn");
        _manageScene = GD.Load<PackedScene>("res://Scenes/Manage.tscn");
        _recruitScene = GD.Load<PackedScene>("res://Scenes/Recruit.tscn");
        _formationScene = GD.Load<PackedScene>("res://Scenes/Formation.tscn");
        _settingsScene = GD.Load<PackedScene>("res://Scenes/Settings.tscn");
        BuildUi();

        long now = (long)Time.GetUnixTimeFromSystem();
        _session.LoadOrCreate(now);
        _session.ApplyOfflineProgress(now);

        // Aplica los ajustes guardados (escala de UI + siempre encima) y fija el tamaño.
        _uiScale = _session.State.UiScale;
        GetWindow().AlwaysOnTop = _session.State.AlwaysOnTop;
        SetWindowSize(CompactSize);

        _arena.Begin(_session);

        // Heartbeat de farming en vivo: cada 10 s acumula el botín del tiempo abierto.
        var heartbeat = new Timer { WaitTime = 10, Autostart = true, OneShot = false };
        AddChild(heartbeat);
        heartbeat.Timeout += OnFarmingHeartbeat;

        Refresh();
    }

    private void OnFarmingHeartbeat()
    {
        long now = (long)Time.GetUnixTimeFromSystem();
        _session.TickLiveFarming(now);
        if (_activeBattle is null && _activeManage is null && _activeRecruit is null && _activeFormation is null)
            Refresh();
    }

    private void BuildUi()
    {
        _mainPanel = new PanelContainer();
        _mainPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_mainPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        _mainPanel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 5);
        margin.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);

        var title = new Label { Text = "🐾 Taskbar Tamer" };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);

        var settings = new Button { Text = "⚙", FocusMode = FocusModeEnum.None };
        settings.Pressed += OnSettingsPressed;
        header.AddChild(settings);

        var close = new Button { Text = "✕", FocusMode = FocusModeEnum.None };
        close.Pressed += () => GetTree().Quit();
        header.AddChild(close);

        vbox.AddChild(new HSeparator());

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        vbox.AddChild(_statusLabel);

        _offlineLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _offlineLabel.Modulate = new Color(0.6f, 0.9f, 0.6f);
        vbox.AddChild(_offlineLabel);

        _arena = new ArenaView();
        _arena.SizeFlagsVertical = SizeFlags.ExpandFill;
        _arena.StageAdvanced += Refresh;
        vbox.AddChild(_arena);

        var farmButton = new Button { Text = "Farmear +1 h", FocusMode = FocusModeEnum.None };
        farmButton.Pressed += OnFarmPressed;
        vbox.AddChild(farmButton);

        var rowManage = new HBoxContainer();
        vbox.AddChild(rowManage);

        var manageButton = new Button { Text = "🧬 Gestionar", FocusMode = FocusModeEnum.None };
        manageButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        manageButton.Pressed += OnManagePressed;
        rowManage.AddChild(manageButton);

        var recruitButton = new Button { Text = "🔓 Desbloquear", FocusMode = FocusModeEnum.None };
        recruitButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        recruitButton.Pressed += OnRecruitPressed;
        rowManage.AddChild(recruitButton);

        var rowCombat = new HBoxContainer();
        vbox.AddChild(rowCombat);

        var formationButton = new Button { Text = "🛡 Formación", FocusMode = FocusModeEnum.None };
        formationButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        formationButton.Pressed += OnFormationPressed;
        rowCombat.AddChild(formationButton);

        var battleButton = new Button { Text = "⚔ Batalla", FocusMode = FocusModeEnum.None };
        battleButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        battleButton.Pressed += OnBattlePressed;
        rowCombat.AddChild(battleButton);

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

    private void OnBattlePressed()
    {
        if (_activeBattle is not null)
            return;

        Setup? player = _session.BuildPlayerSetup();
        if (player is null)
        {
            _statusLabel.Text = "Coloca al menos una criatura en la formación (🛡) para luchar.";
            return;
        }
        Setup rival = Content.RivalSetup();

        _activeBattle = _battleScene.Instantiate<Battle>();
        _activeBattle.Closed += OnBattleClosed;
        AddChild(_activeBattle);

        _mainPanel.Visible = false;
        SetWindowSize(BattleSize);
        _activeBattle.Begin(player, rival, seed: 2024, SetRegistry.Empty, GameConfig.Default);
    }

    private void OnBattleClosed()
    {
        _activeBattle = null;
        _mainPanel.Visible = true;
        SetWindowSize(CompactSize);
        Refresh();
    }

    private void OnManagePressed()
    {
        if (_activeManage is not null)
            return;

        _activeManage = _manageScene.Instantiate<ManagementPanel>();
        _activeManage.Closed += OnManageClosed;
        AddChild(_activeManage);

        _mainPanel.Visible = false;
        SetWindowSize(ManageSize);
        _activeManage.Begin(_session);
    }

    private void OnManageClosed()
    {
        _activeManage = null;
        _mainPanel.Visible = true;
        SetWindowSize(CompactSize);
        _arena.RebuildUnits();
        Refresh();
    }

    private void OnRecruitPressed()
    {
        if (_activeRecruit is not null)
            return;

        _activeRecruit = _recruitScene.Instantiate<RecruitPanel>();
        _activeRecruit.Closed += OnRecruitClosed;
        AddChild(_activeRecruit);

        _mainPanel.Visible = false;
        SetWindowSize(RecruitSize);
        _activeRecruit.Begin(_session);
    }

    private void OnRecruitClosed()
    {
        _activeRecruit = null;
        _mainPanel.Visible = true;
        SetWindowSize(CompactSize);
        _arena.RebuildUnits();
        Refresh();
    }

    private void OnFormationPressed()
    {
        if (_activeFormation is not null)
            return;

        _activeFormation = _formationScene.Instantiate<FormationPanel>();
        _activeFormation.Closed += OnFormationClosed;
        AddChild(_activeFormation);

        _mainPanel.Visible = false;
        SetWindowSize(FormationSize);
        _activeFormation.Begin(_session);
    }

    private void OnFormationClosed()
    {
        _activeFormation = null;
        _mainPanel.Visible = true;
        SetWindowSize(CompactSize);
        _arena.RebuildUnits();
        Refresh();
    }

    private void OnSettingsPressed()
    {
        if (_activeSettings is not null)
            return;

        _activeSettings = _settingsScene.Instantiate<SettingsPanel>();
        _activeSettings.Closed += OnSettingsClosed;
        AddChild(_activeSettings);

        _mainPanel.Visible = false;
        SetWindowSize(SettingsSize);
        _activeSettings.Begin(_uiScale, GetWindow().AlwaysOnTop, OnScaleChanged, OnAlwaysOnTopChanged);
    }

    private void OnSettingsClosed()
    {
        _activeSettings = null;
        _mainPanel.Visible = true;
        SetWindowSize(CompactSize);
        Refresh();
    }

    private void OnScaleChanged(float scale)
    {
        _uiScale = scale;
        _session.State.UiScale = scale;
        _session.Save();
        SetWindowSize(SettingsSize); // re-aplica en vivo mientras el panel está abierto
    }

    private void OnAlwaysOnTopChanged(bool on)
    {
        GetWindow().AlwaysOnTop = on;
        _session.State.AlwaysOnTop = on;
        _session.Save();
    }

    // Aplica la escala de UI: agranda la ventana y escala todo el contenido proporcionalmente.
    private void SetWindowSize(Vector2I baseSize)
    {
        GetWindow().ContentScaleFactor = _uiScale;
        GetWindow().Size = new Vector2I(
            (int)(baseSize.X * _uiScale),
            (int)(baseSize.Y * _uiScale));
    }

    private void Refresh()
    {
        var state = _session.State;
        _statusLabel.Text =
            $"Fase {_session.Stage}  ·  {state.Roster.Count} criatura(s) · poder {_session.TeamPower}\n" +
            $"Inventario: {state.Inventory.Count} partes · Esencia: {state.Essence}";

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

    // Arrastre de la ventana sin bordes. Usa la posición ABSOLUTA del ratón en pantalla
    // (no el delta relativo): así es 1:1 y no se ve afectado por la escala de UI
    // (ContentScaleFactor reportaría el delta relativo en coordenadas de contenido).
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (mb.Pressed)
            {
                _dragMouseStart = DisplayServer.MouseGetPosition();
                _dragWindowStart = GetWindow().Position;
            }
        }
        else if (@event is InputEventMouseMotion && _dragging)
        {
            GetWindow().Position = _dragWindowStart + (DisplayServer.MouseGetPosition() - _dragMouseStart);
        }
    }
}
