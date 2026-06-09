using Godot;
using System;
using TaskbarTamer.Core.Data;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Colección de criaturas: existe <b>una criatura por habilidad</b> y se <b>desbloquean</b>
/// con esencia genética (no se reclutan al azar). Muestra las especies, su habilidad y su
/// estado (desbloqueada o coste).
/// </summary>
public partial class RecruitPanel : Control
{
    public event Action? Closed;

    private GameSession _session = null!;
    private Label _essenceLabel = null!;
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
        var title = new Label { Text = "🔓 Desbloquear criaturas", ClipText = true };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        header.AddChild(back);

        _essenceLabel = new Label();
        root.AddChild(_essenceLabel);

        var hint = new Label { Text = "Hay 1 criatura por habilidad. Fármea esencia para desbloquearlas." };
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
        _listBox.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_listBox);
    }

    private void Refresh()
    {
        _essenceLabel.Text = $"Esencia genética: {_session.State.Essence}";

        foreach (Node child in _listBox.GetChildren())
        {
            _listBox.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Archetype a in _session.AllArchetypes)
            _listBox.AddChild(BuildRow(a));
    }

    private Control BuildRow(Archetype a)
    {
        bool unlocked = _session.IsUnlocked(a);

        var panel = new PanelContainer();
        var m = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            m.AddThemeConstantOverride($"margin_{side}", 8);
        panel.AddChild(m);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        m.AddChild(row);

        var info = new Label
        {
            Text = $"{Content.SpeciesName(a)}\n✨ {Labels.AbilityName(a)}",
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        if (!unlocked)
            info.Modulate = new Color(0.7f, 0.7f, 0.7f);
        row.AddChild(info);

        if (unlocked)
        {
            row.AddChild(new Label { Text = "✓ Desbloqueada", Modulate = new Color(0.5f, 0.95f, 0.6f) });
        }
        else
        {
            var btn = new Button
            {
                Text = $"🔓 {_session.UnlockCost(a)}",
                FocusMode = FocusModeEnum.None,
                Disabled = !_session.CanUnlock(a),
                TooltipText = _session.CanUnlock(a) ? "Desbloquear" : "Necesitas más esencia",
            };
            Archetype arch = a;
            btn.Pressed += () => { _session.Unlock(arch); Refresh(); };
            row.AddChild(btn);
        }

        return panel;
    }

    private void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }
}
