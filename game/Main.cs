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
    private static readonly Vector2I CompactSize = new(360, 430);
    private static readonly Vector2I BattleSize = new(560, 400);
    private static readonly Vector2I ManageSize = new(680, 480);
    private static readonly Vector2I RecruitSize = new(380, 420);
    private static readonly Vector2I FormationSize = new(460, 440);

    private readonly GameSession _session = new();
    private bool _dragging;
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

    public override void _Ready()
    {
        _battleScene = GD.Load<PackedScene>("res://Scenes/Battle.tscn");
        _manageScene = GD.Load<PackedScene>("res://Scenes/Manage.tscn");
        _recruitScene = GD.Load<PackedScene>("res://Scenes/Recruit.tscn");
        _formationScene = GD.Load<PackedScene>("res://Scenes/Formation.tscn");
        BuildUi();

        // La ventana arranca con el tamaño compacto correcto (no el de project.godot).
        GetWindow().Size = CompactSize;

        long now = (long)Time.GetUnixTimeFromSystem();
        _session.LoadOrCreate(now);
        _session.ApplyOfflineProgress(now);

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

        var recruitButton = new Button { Text = "➕ Reclutar", FocusMode = FocusModeEnum.None };
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
        GetWindow().Size = BattleSize;
        _activeBattle.Begin(player, rival, seed: 2024, SetRegistry.Empty, GameConfig.Default);
    }

    private void OnBattleClosed()
    {
        _activeBattle = null;
        _mainPanel.Visible = true;
        GetWindow().Size = CompactSize;
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
        GetWindow().Size = ManageSize;
        _activeManage.Begin(_session);
    }

    private void OnManageClosed()
    {
        _activeManage = null;
        _mainPanel.Visible = true;
        GetWindow().Size = CompactSize;
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
        GetWindow().Size = RecruitSize;
        _activeRecruit.Begin(_session);
    }

    private void OnRecruitClosed()
    {
        _activeRecruit = null;
        _mainPanel.Visible = true;
        GetWindow().Size = CompactSize;
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
        GetWindow().Size = FormationSize;
        _activeFormation.Begin(_session);
    }

    private void OnFormationClosed()
    {
        _activeFormation = null;
        _mainPanel.Visible = true;
        GetWindow().Size = CompactSize;
        Refresh();
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
