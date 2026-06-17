using Godot;
using System.Collections.Generic;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Genera sprites pixel-art de las criaturas por código (Image de baja resolución +
/// paleta sombreada + contorno + ojos + rasgo por especie) y los hornea a textura. Se
/// dibujan con filtro nearest-neighbor → pixelado nítido, sin assets externos.
/// </summary>
public static class PixelSpriteFactory
{
    public const int Size = 20;

    public static Color ColorFor(Archetype a) => a switch
    {
        Archetype.Guardian => new Color(0.40f, 0.60f, 1.00f),  // azul
        Archetype.Bruiser => new Color(1.00f, 0.52f, 0.30f),   // naranja
        Archetype.Charger => new Color(0.40f, 0.95f, 0.95f),   // cian
        Archetype.Leaper => new Color(0.70f, 0.50f, 1.00f),    // morado
        Archetype.Venomous => new Color(0.50f, 0.95f, 0.40f),  // verde
        _ => new Color(0.72f, 0.72f, 0.72f),
    };

    public static ImageTexture BakeArchetype(Archetype a)
    {
        Color b = ColorFor(a);
        Image img = NewImg();
        FillBody(img, 10f, 11f, 7f, 6f, b);
        Color feat = Dark(b);

        switch (a)
        {
            case Archetype.Bruiser: // cuernos
                Horn(img, 5, 4, -1, feat);
                Horn(img, 14, 4, 1, feat);
                break;

            case Archetype.Charger: // aleta superior
                P(img, 10, 3, feat); P(img, 10, 2, feat); P(img, 11, 1, feat);
                break;

            case Archetype.Leaper: // patas (resorte)
                P(img, 6, 17, feat); P(img, 6, 18, feat);
                P(img, 13, 17, feat); P(img, 13, 18, feat);
                break;

            case Archetype.Venomous: // antena con glándula
                P(img, 10, 4, feat); P(img, 10, 3, feat); P(img, 10, 2, feat);
                P(img, 10, 1, new Color(0.6f, 1f, 0.4f));
                break;

            default: // Guardian: visera blindada
                Color visor = Dark(Dark(b));
                for (int x = 5; x <= 14; x++) P(img, x, 8, visor);
                break;
        }

        Outline(img, Out(b));
        Eyes(img, new Color(0.1f, 0.1f, 0.15f));
        return ImageTexture.CreateFromImage(img);
    }

    public static ImageTexture BakeBlob(Color baseCol, bool boss)
    {
        Image img = NewImg();
        FillBody(img, 10f, 11f, boss ? 8f : 7f, boss ? 7.5f : 6.5f, baseCol);
        Color feat = Dark(baseCol);

        // Pinchos.
        P(img, 6, 4, feat); P(img, 10, 3, feat); P(img, 14, 4, feat);
        if (boss)
        {
            Horn(img, 5, 4, -1, feat);
            Horn(img, 14, 4, 1, feat);
        }

        Outline(img, Out(baseCol));
        Eyes(img, new Color(0.85f, 0.12f, 0.12f)); // ojos rojos
        return ImageTexture.CreateFromImage(img);
    }

    // ---------- helpers ----------

    private static Image NewImg() => Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
    private static Color Light(Color c) => c.Lerp(Colors.White, 0.35f);
    private static Color Dark(Color c) => c.Lerp(Colors.Black, 0.30f);
    private static Color Out(Color c) => c.Lerp(Colors.Black, 0.62f);

    private static void P(Image img, int x, int y, Color c)
    {
        if (x >= 0 && y >= 0 && x < Size && y < Size)
            img.SetPixel(x, y, c);
    }

    private static void FillBody(Image img, float cx, float cy, float rx, float ry, Color b)
    {
        Color top = Light(b), mid = b, bot = Dark(b);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dx * dx + dy * dy > 1f) continue;
                float ny = (y - (cy - ry)) / (2f * ry); // 0 arriba .. 1 abajo
                Color c = ny < 0.33f ? top : ny > 0.72f ? bot : mid;
                img.SetPixel(x, y, c);
            }
    }

    private static void Horn(Image img, int x, int y, int dir, Color c)
    {
        P(img, x, y, c); P(img, x, y - 1, c); P(img, x + dir, y - 1, c);
    }

    // Añade un contorno de 1px alrededor de la silueta.
    private static void Outline(Image img, Color outline)
    {
        var pts = new List<(int, int)>();
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                if (img.GetPixel(x, y).A > 0.01f) continue;
                bool adj =
                    (x > 0 && img.GetPixel(x - 1, y).A > 0.01f) ||
                    (x < Size - 1 && img.GetPixel(x + 1, y).A > 0.01f) ||
                    (y > 0 && img.GetPixel(x, y - 1).A > 0.01f) ||
                    (y < Size - 1 && img.GetPixel(x, y + 1).A > 0.01f);
                if (adj) pts.Add((x, y));
            }
        foreach ((int x, int y) in pts)
            img.SetPixel(x, y, outline);
    }

    private static void Eyes(Image img, Color pupil)
    {
        P(img, 8, 9, Colors.White); P(img, 11, 9, Colors.White);
        P(img, 8, 10, pupil); P(img, 11, 10, pupil);
    }
}
