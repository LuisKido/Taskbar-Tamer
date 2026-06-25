using Godot;
using System.Collections.Generic;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Carga sprites de criatura desde <c>res://assets/creatures/</c> (PNG en hoja 4×2 de 8
/// cuadros, mirando a la izquierda). Recorta un cuadro y lo reescala (suave) al tamaño
/// pedido. Si el PNG no existe/aún no está importado, devuelve null → la arena usa el
/// sprite generado por código como respaldo.
/// </summary>
public static class CreatureArt
{
    private const int Cols = 4;
    private const int Rows = 2;

    private static readonly Dictionary<Archetype, string> Files = new()
    {
        [Archetype.Guardian] = "mordak",
        [Archetype.Bruiser] = "rendkar",
        [Archetype.Charger] = "voltfang",
        [Archetype.Leaper] = "skarn",
        [Archetype.Venomous] = "toxia",
    };

    private static string? Path(Archetype a)
        => Files.TryGetValue(a, out string? n) ? $"res://assets/creatures/{n}.png" : null;

    public static bool HasAsset(Archetype a)
    {
        string? p = Path(a);
        return p is not null && ResourceLoader.Exists(p);
    }

    /// <summary>Cuadro (col, fila) reescalado a <paramref name="width"/> px (alto proporcional). Null si no hay asset.</summary>
    public static ImageTexture? Frame(Archetype a, int col, int row, int width)
    {
        string? p = Path(a);
        if (p is null || !ResourceLoader.Exists(p))
            return null;

        var tex = GD.Load<Texture2D>(p);
        Image full = tex.GetImage();
        if (full.GetFormat() != Image.Format.Rgba8)
            full.Convert(Image.Format.Rgba8);

        int cw = full.GetWidth() / Cols;
        int ch = full.GetHeight() / Rows;
        Image frame = full.GetRegion(new Rect2I(col * cw, row * ch, cw, ch));

        int h = Mathf.Max(1, Mathf.RoundToInt(width * (float)ch / cw));
        frame.Resize(width, h, Image.Interpolation.Lanczos);
        return ImageTexture.CreateFromImage(frame);
    }

    /// <summary>Cuadro "idle" (esquina superior izquierda) para la arena.</summary>
    public static ImageTexture? ArenaTexture(Archetype a, int width) => Frame(a, 0, 0, width);
}
