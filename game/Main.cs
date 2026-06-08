using Godot;
using System;
using System.Collections.Generic;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Pantalla compacta de Taskbar Tamer. Es una ventana sin bordes que el usuario
/// arrastra donde quiera. Sirve de prueba de integración Godot↔core: construye un
/// equipo, calcula su poder y resuelve una sesión de farming con la lógica de core/.
/// </summary>
public partial class Main : Control
{
    private bool _dragging;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        BuildUi();
        RunFarmingDemo();
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
        vbox.AddThemeConstantOverride("separation", 6);
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

        var hint = new Label { Text = "Arrastra para mover la ventana." };
        hint.Modulate = new Color(1, 1, 1, 0.5f);
        vbox.AddChild(hint);
    }

    /// <summary>Usa core/ para construir un equipo y resolver 1 hora de farming.</summary>
    private void RunFarmingDemo()
    {
        GameConfig config = GameConfig.Default;

        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(1, "Abisal", AnatomySlot.Claws, Rarity.Raro, config),
            [AnatomySlot.Shell] = PartFactory.Create(2, "Abisal", AnatomySlot.Shell, Rarity.Comun, config),
        };
        var hero = new Creature(100, "Mordak", new Stats(300, 20, 10, 15, 0, 0, 0, 0), equipped);
        var setup = new Setup(new[] { hero }, Array.Empty<Creature>());
        int power = PowerRating.Of(setup, SetRegistry.Empty);

        var biome = new Biome("Bosque Abisal", requiredPower: 0, new[]
        {
            new LootEntry("Abisal", AnatomySlot.Fangs, Rarity.Comun, 70),
            new LootEntry("Abisal", AnatomySlot.Claws, Rarity.Raro, 25),
            new LootEntry("Abisal", AnatomySlot.Stinger, Rarity.Epico, 5),
        });

        var ids = new IdAllocator(1000);
        FarmingResult result = FarmingSimulator.Resolve(
            new FarmingAssignment(power, RngState: 12345), biome, elapsedSeconds: 3600, ids, config);

        int epicos = 0;
        foreach (Part p in result.Loot)
            if (p.Rarity == Rarity.Epico) epicos++;

        _statusLabel.Text =
            $"{hero.Name}  ·  poder {power}\n" +
            $"Bioma: {biome.Id}\n" +
            $"1 h → {result.Encounters} encuentros\n" +
            $"Botín: {result.Loot.Count}  (épicos: {epicos})\n" +
            $"XP: {result.XpGained}";
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
