using Godot;
using System;
using System.Collections.Generic;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Reproductor de batalla: ejecuta <see cref="BattleSimulator"/> una vez y luego
/// <b>anima</b> el log de eventos (golpes, críticos, veneno, muertes). No recalcula
/// nada: solo reproduce lo que core/ ya resolvió de forma determinista.
/// </summary>
public partial class Battle : Control
{
    private const float SecondsPerEvent = 0.55f;

    /// <summary>Se emite al cerrar el reproductor (botón Volver o fin + cerrar).</summary>
    public event Action? Closed;

    private sealed class View
    {
        public required PanelContainer Panel;
        public required Label NameLabel;
        public required ProgressBar Bar;
        public required Label HpLabel;
        public required int MaxHp;
        public int CurrentHp;
        public bool Dead;
    }

    private readonly Dictionary<long, View> _views = new();
    private readonly Dictionary<long, string> _names = new();

    private BattleResult _result = null!;
    private int _eventIndex;
    private Timer _timer = null!;
    private Label _narration = null!;
    private Label _outcomeLabel = null!;

    /// <summary>Arranca la batalla: simula y comienza la reproducción.</summary>
    public void Begin(Setup player, Setup rival, ulong seed, SetRegistry sets, GameConfig config)
    {
        _result = BattleSimulator.Simulate(player, rival, seed, sets, config);

        BuildUi(player, rival, sets);

        _timer = new Timer { WaitTime = SecondsPerEvent, OneShot = false };
        AddChild(_timer);
        _timer.Timeout += PlayNextEvent;
        _timer.Start();
    }

    private void BuildUi(Setup player, Setup rival, SetRegistry sets)
    {
        // Fondo opaco que tapa por completo el panel principal de detrás.
        var backdrop = new ColorRect { Color = new Color(0.11f, 0.11f, 0.13f) };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var bg = new PanelContainer();
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 12);
        bg.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var title = new Label { Text = "⚔ Batalla", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(title);

        var arena = new HBoxContainer();
        arena.SizeFlagsVertical = SizeFlags.ExpandFill;
        arena.AddThemeConstantOverride("separation", 10);
        root.AddChild(arena);

        arena.AddChild(BuildTeamColumn("TU EQUIPO", player, sets, new Color(0.6f, 0.85f, 1f)));
        var sep = new VSeparator();
        arena.AddChild(sep);
        arena.AddChild(BuildTeamColumn("RIVAL", rival, sets, new Color(1f, 0.7f, 0.7f)));

        _narration = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _narration.CustomMinimumSize = new Vector2(0, 32);
        root.AddChild(_narration);

        var footer = new HBoxContainer();
        root.AddChild(footer);

        _outcomeLabel = new Label { Text = "" };
        _outcomeLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(_outcomeLabel);

        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        footer.AddChild(back);
    }

    private Control BuildTeamColumn(string heading, Setup setup, SetRegistry sets, Color tint)
    {
        var col = new VBoxContainer();
        col.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        col.AddThemeConstantOverride("separation", 6);

        var head = new Label { Text = heading, HorizontalAlignment = HorizontalAlignment.Center };
        head.Modulate = tint;
        col.AddChild(head);

        foreach (Creature creature in setup.All)
            col.AddChild(BuildCombatantView(creature, sets));

        return col;
    }

    private Control BuildCombatantView(Creature creature, SetRegistry sets)
    {
        int maxHp = StatsResolver.Resolve(creature, sets).Stats.MaxHp;

        var panel = new PanelContainer();
        var inner = new VBoxContainer();
        inner.AddThemeConstantOverride("separation", 2);
        var m = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            m.AddThemeConstantOverride($"margin_{side}", 6);
        m.AddChild(inner);
        panel.AddChild(m);

        var name = new Label { Text = creature.Name };
        inner.AddChild(name);

        var bar = new ProgressBar { MinValue = 0, MaxValue = maxHp, Value = maxHp, ShowPercentage = false };
        bar.CustomMinimumSize = new Vector2(0, 14);
        inner.AddChild(bar);

        var hp = new Label { Text = $"{maxHp}/{maxHp}" };
        hp.AddThemeFontSizeOverride("font_size", 11);
        inner.AddChild(hp);

        _views[creature.Id] = new View
        {
            Panel = panel,
            NameLabel = name,
            Bar = bar,
            HpLabel = hp,
            MaxHp = maxHp,
            CurrentHp = maxHp,
        };
        _names[creature.Id] = creature.Name;
        return panel;
    }

    private void PlayNextEvent()
    {
        if (_eventIndex >= _result.Log.Count)
        {
            Finish();
            return;
        }

        BattleEvent e = _result.Log[_eventIndex++];
        switch (e.Type)
        {
            case BattleEventType.Attack:
                ApplyDamage(e.TargetId, e.Value);
                Flash(e.TargetId, new Color(1f, 0.4f, 0.4f));
                _narration.Text = e.Crit
                    ? $"💥 {Name(e.ActorId)} CRÍTICO a {Name(e.TargetId)}: {e.Value}"
                    : $"{Name(e.ActorId)} golpea a {Name(e.TargetId)}: {e.Value}";
                break;

            case BattleEventType.Evade:
                _narration.Text = $"{Name(e.TargetId)} esquiva a {Name(e.ActorId)}";
                Flash(e.TargetId, new Color(0.8f, 0.8f, 0.4f));
                break;

            case BattleEventType.PoisonTick:
                ApplyDamage(e.TargetId, e.Value);
                Flash(e.TargetId, new Color(0.5f, 1f, 0.5f));
                _narration.Text = $"☠ {Name(e.TargetId)} sufre {e.Value} de veneno";
                break;

            case BattleEventType.Death:
                MarkDead(e.TargetId);
                _narration.Text = $"☠ {Name(e.TargetId)} cae derrotado";
                break;
        }
    }

    private void ApplyDamage(long id, int amount)
    {
        if (!_views.TryGetValue(id, out View? v))
            return;
        v.CurrentHp = Math.Max(0, v.CurrentHp - amount);
        v.Bar.Value = v.CurrentHp;
        v.HpLabel.Text = $"{v.CurrentHp}/{v.MaxHp}";
    }

    private void MarkDead(long id)
    {
        if (!_views.TryGetValue(id, out View? v) || v.Dead)
            return;
        v.Dead = true;
        v.CurrentHp = 0;
        v.Bar.Value = 0;
        v.HpLabel.Text = "K.O.";
        v.Panel.Modulate = new Color(0.45f, 0.45f, 0.45f);
    }

    private void Flash(long id, Color color)
    {
        if (!_views.TryGetValue(id, out View? v) || v.Dead)
            return;
        v.Panel.Modulate = color;
        Tween t = CreateTween();
        t.TweenProperty(v.Panel, "modulate", Colors.White, 0.35f);
    }

    private void Finish()
    {
        _timer.Stop();
        string text = _result.Outcome switch
        {
            BattleOutcome.TeamA => "🏆 ¡Victoria!",
            BattleOutcome.TeamB => "Derrota…",
            _ => "Empate",
        };
        _outcomeLabel.Text = $"{text}  ({_result.Rounds} rondas)";
        _narration.Text = "Fin de la batalla.";
    }

    private string Name(long id) => _names.TryGetValue(id, out string? n) ? n : $"#{id}";

    private void Close()
    {
        _timer?.Stop();
        Closed?.Invoke();
        QueueFree();
    }
}
