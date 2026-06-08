using Godot;
using System;
using System.Collections.Generic;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Panel de gestión (fase activa): muestra el roster, las ranuras de anatomía de la
/// criatura seleccionada y el inventario. Permite equipar partes del inventario y
/// desequiparlas. Cada acción pasa por <see cref="GameSession"/> (que usa core/) y
/// persiste; la UI solo refleja el estado.
/// </summary>
public partial class ManagementPanel : Control
{
    public event Action? Closed;

    private GameSession _session = null!;
    private int _selected;

    private Label _powerLabel = null!;
    private VBoxContainer _rosterBox = null!;
    private VBoxContainer _slotsBox = null!;
    private Label _invCountLabel = null!;
    private VBoxContainer _invBox = null!;
    private int _lastFusions = -1;

    public void Begin(GameSession session)
    {
        _session = session;
        BuildUi();
        RefreshAll();
    }

    private void BuildUi()
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

        // Cabecera
        var header = new HBoxContainer();
        root.AddChild(header);

        var title = new Label { Text = "🧬 Gestión de equipo" };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);

        _powerLabel = new Label();
        header.AddChild(_powerLabel);

        var back = new Button { Text = "Volver", FocusMode = FocusModeEnum.None };
        back.Pressed += Close;
        header.AddChild(back);

        root.AddChild(new HSeparator());

        // Cuerpo: izquierda (criatura + ranuras) | derecha (inventario)
        var body = new HBoxContainer();
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 12);
        root.AddChild(body);

        var left = new VBoxContainer();
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        left.AddThemeConstantOverride("separation", 6);
        body.AddChild(left);

        left.AddChild(new Label { Text = "Criaturas" });
        _rosterBox = new VBoxContainer();
        left.AddChild(_rosterBox);

        left.AddChild(new Label { Text = "Ranuras de anatomía" });
        _slotsBox = new VBoxContainer();
        _slotsBox.AddThemeConstantOverride("separation", 2);
        left.AddChild(_slotsBox);

        body.AddChild(new VSeparator());

        var right = new VBoxContainer();
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(right);

        var invHeader = new HBoxContainer();
        right.AddChild(invHeader);

        _invCountLabel = new Label { Text = "Inventario" };
        _invCountLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        invHeader.AddChild(_invCountLabel);

        var fuseButton = new Button { Text = "⚗ Fusionar todo", FocusMode = FocusModeEnum.None };
        fuseButton.TooltipText = $"Combina {GameConfig.Default.FusionRequirement} partes idénticas en una de rareza superior";
        fuseButton.Pressed += OnFusePressed;
        invHeader.AddChild(fuseButton);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        right.AddChild(scroll);

        _invBox = new VBoxContainer();
        _invBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _invBox.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_invBox);
    }

    private void RefreshAll()
    {
        var state = _session.State;
        if (_selected >= state.Roster.Count)
            _selected = 0;

        _powerLabel.Text = $"Poder de equipo: {_session.TeamPower}";

        RefreshRoster();
        RefreshSlots();
        RefreshInventory();
    }

    private void RefreshRoster()
    {
        ClearChildren(_rosterBox);
        var roster = _session.State.Roster;
        for (int i = 0; i < roster.Count; i++)
        {
            Creature c = roster[i];
            int power = PowerRating.Of(new[] { c }, SetRegistry.Empty);
            var btn = new Button
            {
                Text = $"{c.Name}  ·  poder {power}",
                FocusMode = FocusModeEnum.None,
                ToggleMode = true,
                ButtonPressed = i == _selected,
                ClipText = true,
            };
            int index = i;
            btn.Pressed += () => { _selected = index; RefreshAll(); };
            _rosterBox.AddChild(btn);
        }
    }

    private void RefreshSlots()
    {
        ClearChildren(_slotsBox);
        if (_session.State.Roster.Count == 0)
            return;

        Creature creature = _session.State.Roster[_selected];
        foreach (AnatomySlot slot in Enum.GetValues<AnatomySlot>())
        {
            bool hasPart = creature.Equipped.TryGetValue(slot, out Part? part);
            string detail = hasPart ? $"{part!.Family} [{Labels.Rarity(part.Rarity)}]" : "—";

            var btn = new Button
            {
                Text = $"{Labels.Slot(slot)}: {detail}",
                FocusMode = FocusModeEnum.None,
                Disabled = !hasPart,
                Alignment = HorizontalAlignment.Left,
                ClipText = true,
                TooltipText = hasPart ? "Clic para desequipar" : "Ranura vacía",
            };
            if (hasPart)
            {
                btn.Modulate = Labels.RarityColor(part!.Rarity);
                AnatomySlot s = slot;
                btn.Pressed += () => { _session.Unequip(_selected, s); RefreshAll(); };
            }
            _slotsBox.AddChild(btn);
        }
    }

    private void OnFusePressed()
    {
        _lastFusions = _session.FuseAll();
        RefreshAll();
    }

    private void RefreshInventory()
    {
        ClearChildren(_invBox);
        var inventory = _session.State.Inventory;

        string suffix = _lastFusions switch
        {
            > 0 => $"  ·  ⚗ {_lastFusions} fusiones",
            0 => "  ·  nada que fusionar",
            _ => "  ·  clic para equipar",
        };
        _invCountLabel.Text = $"Inventario ({inventory.Count}){suffix}";

        // Agrupa partes idénticas (misma familia+ranura+rareza) en orden de aparición.
        var order = new List<PartKind>();
        var reps = new Dictionary<PartKind, Part>();
        var counts = new Dictionary<PartKind, int>();
        foreach (Part part in inventory)
        {
            if (counts.TryGetValue(part.Kind, out int n))
            {
                counts[part.Kind] = n + 1;
            }
            else
            {
                order.Add(part.Kind);
                reps[part.Kind] = part;
                counts[part.Kind] = 1;
            }
        }

        int fusionReq = GameConfig.Default.FusionRequirement;
        foreach (PartKind kind in order)
        {
            Part rep = reps[kind];
            int count = counts[kind];
            bool fusable = count >= fusionReq && kind.Rarity != Rarity.Legendario;
            string mark = fusable ? "  ⚗" : "";

            var btn = new Button
            {
                Text = $"{Labels.Slot(kind.Slot)} {kind.Family} [{Labels.Rarity(kind.Rarity)}] ×{count}{mark}\n{Labels.PartStats(rep)}",
                FocusMode = FocusModeEnum.None,
                Alignment = HorizontalAlignment.Left,
                ClipText = true,
                TooltipText = "Clic para equipar una",
            };
            btn.Modulate = Labels.RarityColor(kind.Rarity);
            btn.AddThemeFontSizeOverride("font_size", 11);

            Part toEquip = rep;
            btn.Pressed += () => { _session.Equip(_selected, toEquip); RefreshAll(); };
            _invBox.AddChild(btn);
        }

        _lastFusions = -1; // el mensaje de fusión se muestra una sola vez
    }

    private static void ClearChildren(Node box)
    {
        foreach (Node child in box.GetChildren())
        {
            box.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }
}
