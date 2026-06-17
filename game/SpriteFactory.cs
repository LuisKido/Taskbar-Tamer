using Godot;
using System;

namespace TaskbarTamer.Game;

/// <summary>
/// Genera texturas de criatura por código y las hornea a <see cref="ImageTexture"/> una
/// sola vez (no se recalculan cada frame). Prototipo de "sprites generados por código":
/// una esfera sombreada (luz/sombra + borde) en escala de grises que luego se <b>tinta</b>
/// con el color de cada criatura, dándole volumen sin assets externos.
/// </summary>
public static class SpriteFactory
{
    /// <summary>
    /// Hornea una esfera sombreada de <paramref name="size"/> px. La intensidad va en RGB
    /// (gris) y el recorte redondo en alfa; se usa con modulate=color para tintar.
    /// </summary>
    public static ImageTexture BakeSphere(int size)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        float c = (size - 1) / 2f;
        float rad = c;
        Vector3 light = new Vector3(-0.5f, -0.6f, 0.75f).Normalized(); // arriba-izquierda, al frente

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / rad;
                float dy = (y - c) / rad;
                float d2 = dx * dx + dy * dy;
                if (d2 > 1f)
                {
                    img.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    continue;
                }

                float dz = MathF.Sqrt(1f - d2);
                var normal = new Vector3(dx, dy, dz);
                float diff = MathF.Max(0f, normal.Dot(light));
                float bright = 0.40f + 0.80f * diff; // ambiente + difuso

                float edge = MathF.Sqrt(d2);
                if (edge > 0.86f)
                    bright *= 0.45f; // anillo de borde (contorno)

                bright = Math.Clamp(bright, 0f, 1f);
                float alpha = edge > 0.96f ? Math.Clamp(1f - (edge - 0.96f) / 0.04f, 0f, 1f) : 1f;
                img.SetPixel(x, y, new Color(bright, bright, bright, alpha));
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
