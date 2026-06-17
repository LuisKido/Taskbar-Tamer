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
    private PartKind _kind;
    private int _count;
    private bool _fusable;
    private Action? _onClick;

    public void Setup(PartKind kind, int count, bool fusable, string tooltip, Action onClick)
    {
        _kind = kind;
        _count = count;
        _fusable = fusable;
        _onClick = onClick;
        CustomMinimumSize = new Vector2(58, 58);
        TooltipText = tooltip;
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
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
