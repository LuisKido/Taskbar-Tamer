using Godot;
using System;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Editor de formación: coloca cada criatura en la línea frontal (absorbe daño), la
/// retaguardia (protegida) o la banca. La formación se guarda y la usa el simulador
/// de batalla (que ya respeta el posicionamiento).
/// </summary>
public partial class FormationPanel : Control
{
    public event Action? Closed;

    private GameSession _session = null!;
    private Label _countsLabel = null!;
    private VBoxContainer _listBox = null!;

    public void Begin(GameSession session)
    {
        _session = session;
        BuildUi();
        Refresh();
    }

    private void BuildUi()
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
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new HBoxContainer();
        root.AddChild(header);
        var title = new Label { Text = "🛡 Formación", ClipText = true };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        header.AddChild(back);

        _countsLabel = new Label();
        root.AddChild(_countsLabel);

        var hint = new Label
        {
            Text = "Frontal absorbe el daño · la retaguardia queda protegida hasta que cae la frontal.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.Modulate = new Color(1, 1, 1, 0.5f);
        hint.AddThemeFontSizeOverride("font_size", 11);
        root.AddChild(hint);

        root.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(scroll);

        _listBox = new VBoxContainer();
        _listBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _listBox.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_listBox);
    }

    private void Refresh()
    {
        _countsLabel.Text = $"Frontal {_session.FrontCount}/{_session.MaxLine}   ·   Retaguardia {_session.BackCount}/{_session.MaxLine}";

        foreach (Node child in _listBox.GetChildren())
        {
            _listBox.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Creature creature in _session.State.Roster)
            _listBox.AddChild(BuildRow(creature));
    }

    private Control BuildRow(Creature creature)
    {
        FormationZone zone = _session.ZoneOf(creature.Id);
        int power = PowerRating.Of(new[] { creature }, SetRegistry.Empty);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var info = new Label
        {
            Text = $"{creature.Name} · {power} · [{ZoneName(zone)}]",
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        info.Modulate = ZoneColor(zone);
        row.AddChild(info);

        long id = creature.Id;

        var frontBtn = new Button { Text = "Frontal", FocusMode = FocusModeEnum.None };
        frontBtn.Disabled = zone == FormationZone.Front || _session.FrontCount >= _session.MaxLine;
        frontBtn.Pressed += () => { _session.PlaceFront(id); Refresh(); };
        row.AddChild(frontBtn);

        var backBtn = new Button { Text = "Retag.", FocusMode = FocusModeEnum.None };
        backBtn.Disabled = zone == FormationZone.Back || _session.BackCount >= _session.MaxLine;
        backBtn.Pressed += () => { _session.PlaceBack(id); Refresh(); };
        row.AddChild(backBtn);

        var benchBtn = new Button { Text = "Banca", FocusMode = FocusModeEnum.None };
        benchBtn.Disabled = zone == FormationZone.Bench;
        benchBtn.Pressed += () => { _session.Bench(id); Refresh(); };
        row.AddChild(benchBtn);

        return row;
    }

    private static string ZoneName(FormationZone z) => z switch
    {
        FormationZone.Front => "Frontal",
        FormationZone.Back => "Retaguardia",
        _ => "Banca",
    };

    private static Color ZoneColor(FormationZone z) => z switch
    {
        FormationZone.Front => new Color(0.6f, 0.85f, 1f),
        FormationZone.Back => new Color(1f, 0.8f, 0.5f),
        _ => new Color(0.7f, 0.7f, 0.7f),
    };

    private void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }
}
