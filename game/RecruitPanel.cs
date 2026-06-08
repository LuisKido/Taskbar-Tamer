using Godot;
using System;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Pantalla dedicada al reclutamiento de criaturas. Separada de la gestión de equipo:
/// aquí solo se gasta esencia genética para incorporar criaturas nuevas al roster.
/// </summary>
public partial class RecruitPanel : Control
{
    public event Action? Closed;

    private GameSession _session = null!;
    private Label _essenceLabel = null!;
    private Button _recruitButton = null!;
    private Label _resultLabel = null!;
    private VBoxContainer _rosterBox = null!;

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
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        // Cabecera
        var header = new HBoxContainer();
        root.AddChild(header);
        var title = new Label { Text = "🧬 Reclutar criatura", ClipText = true };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        header.AddChild(back);

        root.AddChild(new HSeparator());

        _essenceLabel = new Label();
        root.AddChild(_essenceLabel);

        _recruitButton = new Button { FocusMode = FocusModeEnum.None };
        _recruitButton.Pressed += OnRecruitPressed;
        root.AddChild(_recruitButton);

        _resultLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _resultLabel.Modulate = new Color(0.6f, 0.9f, 0.6f);
        root.AddChild(_resultLabel);

        root.AddChild(new HSeparator());
        root.AddChild(new Label { Text = "Tu equipo" });

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(scroll);

        _rosterBox = new VBoxContainer();
        _rosterBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_rosterBox);
    }

    private void OnRecruitPressed()
    {
        Creature? recruited = _session.Recruit();
        if (recruited is not null)
        {
            Stats s = recruited.Innate;
            _resultLabel.Text = $"¡{recruited.Name} se unió!  ({s.MaxHp} vida · {s.Attack} atk · {s.Speed} vel)";
        }
        Refresh();
    }

    private void Refresh()
    {
        var state = _session.State;
        _essenceLabel.Text = $"Esencia genética: {state.Essence}";

        _recruitButton.Text = $"➕ Reclutar  ·  coste {_session.RecruitCost}";
        _recruitButton.Disabled = !_session.CanRecruit;
        _recruitButton.TooltipText = _session.CanRecruit
            ? "Incorpora una criatura nueva al equipo"
            : "Necesitas más esencia (fármea para conseguirla)";

        foreach (Node child in _rosterBox.GetChildren())
        {
            _rosterBox.RemoveChild(child);
            child.QueueFree();
        }
        foreach (Creature c in state.Roster)
        {
            int power = PowerRating.Of(new[] { c }, SetRegistry.Empty);
            _rosterBox.AddChild(new Label
            {
                Text = $"• {c.Name}  ·  poder {power}",
                ClipText = true,
            });
        }
    }

    private void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }
}
