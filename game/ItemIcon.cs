using Godot;
using System;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Tarjeta de inventario: un cuadro con marco coloreado por rareza y, dentro, el sprite
/// del objeto (dibujado por código según la ranura). Clicable para equipar.
/// </summary>
public partial class ItemIcon : Control
{
    private Part _rep = null!;
    private PartKind _kind;
    private int _count;
    private bool _fusable;
    private Action? _onClick;

    public void Setup(Part rep, int count, bool fusable, Action onClick)
    {
        _rep = rep;
        _kind = rep.Kind;
        _count = count;
        _fusable = fusable;
        _onClick = onClick;
        CustomMinimumSize = new Vector2(58, 58);
        TooltipText = Labels.Slot(rep.Slot); // no vacío → activa el tooltip personalizado
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
    }

    // Tooltip personalizado: un cuadro con marco de rareza y las estadísticas del objeto.
    public override Control _MakeCustomTooltip(string forText)
    {
        Color rc = Labels.RarityColor(_kind.Rarity);

        var sb = new StyleBoxFlat { BgColor = new Color(0.08f, 0.08f, 0.11f, 0.97f), BorderColor = rc };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(5);
        sb.SetContentMarginAll(8);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", sb);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vb);

        var title = new Label { Text = $"{Labels.Slot(_kind.Slot)}  [{Labels.Rarity(_kind.Rarity)}]" };
        title.Modulate = rc;
        vb.AddChild(title);

        var fam = new Label { Text = $"{_rep.Family}   ×{_count}" };
        fam.Modulate = new Color(0.7f, 0.7f, 0.7f);
        fam.AddThemeFontSizeOverride("font_size", 11);
        vb.AddChild(fam);

        vb.AddChild(new HSeparator());

        foreach (string line in Labels.PartStatLines(_rep))
            vb.AddChild(new Label { Text = line });

        var hint = new Label { Text = _fusable ? "⚗ fusionable · clic para equipar" : "Clic para equipar" };
        hint.Modulate = new Color(1, 1, 1, 0.45f);
        hint.AddThemeFontSizeOverride("font_size", 10);
        vb.AddChild(hint);

        return panel;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            _onClick?.Invoke();
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        Vector2 sz = Size;
        Color rarity = Labels.RarityColor(_kind.Rarity);

        // Fondo + marco coloreado por rareza.
        DrawRect(new Rect2(Vector2.Zero, sz), new Color(0.10f, 0.10f, 0.13f));
        DrawRect(new Rect2(new Vector2(1.5f, 1.5f), sz - new Vector2(3f, 3f)), rarity, filled: false, width: 2.5f);

        // Sprite del objeto.
        PartIcons.Draw(this, _kind.Slot, sz * 0.5f, sz.X * 0.30f, rarity);

        // Marca de fusionable.
        if (_fusable)
            DrawCircle(new Vector2(sz.X - 8f, 8f), 4f, new Color(0.5f, 0.95f, 0.45f));

        // Contador.
        if (_count > 1)
        {
            Font font = GetThemeDefaultFont();
            DrawString(font, new Vector2(4f, sz.Y - 4f), $"x{_count}", HorizontalAlignment.Left, -1, 11, Colors.White);
        }
    }
}
