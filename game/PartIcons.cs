using Godot;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Dibuja por código un icono representativo de cada ranura de anatomía (garras,
/// colmillos, caparazón…). Se usa dentro de las tarjetas del inventario.
/// </summary>
public static class PartIcons
{
    public static void Draw(CanvasItem ci, AnatomySlot slot, Vector2 c, float r, Color col)
    {
        Color dark = new(col.R * 0.6f, col.G * 0.6f, col.B * 0.6f);

        switch (slot)
        {
            case AnatomySlot.Claws: // tres garras
                for (int k = -1; k <= 1; k++)
                {
                    float ox = k * r * 0.5f;
                    Tri(ci,
                        c + new Vector2(ox - r * 0.18f, r * 0.55f),
                        c + new Vector2(ox + r * 0.12f, r * 0.45f),
                        c + new Vector2(ox - r * 0.05f, -r * 0.95f), col);
                }
                break;

            case AnatomySlot.Fangs: // dos colmillos
                Tri(ci, c + new Vector2(-r * 0.5f, -r * 0.6f), c + new Vector2(-r * 0.12f, -r * 0.6f), c + new Vector2(-r * 0.3f, r * 0.85f), col);
                Tri(ci, c + new Vector2(r * 0.12f, -r * 0.6f), c + new Vector2(r * 0.5f, -r * 0.6f), c + new Vector2(r * 0.3f, r * 0.85f), col);
                break;

            case AnatomySlot.Stinger: // aguijón
                Tri(ci, c + new Vector2(-r * 0.22f, -r * 0.7f), c + new Vector2(r * 0.22f, -r * 0.7f), c + new Vector2(0f, r * 0.95f), col);
                ci.DrawLine(c + new Vector2(0f, r * 0.1f), c + new Vector2(-r * 0.45f, -r * 0.1f), dark, 2f);
                ci.DrawLine(c + new Vector2(0f, r * 0.1f), c + new Vector2(r * 0.45f, -r * 0.1f), dark, 2f);
                break;

            case AnatomySlot.Shell: // domo
                ci.DrawArc(c + new Vector2(0f, r * 0.35f), r * 0.95f, Mathf.Pi, Mathf.Tau, 18, col, 4f);
                ci.DrawLine(c + new Vector2(-r * 0.9f, r * 0.35f), c + new Vector2(r * 0.9f, r * 0.35f), col, 3f);
                break;

            case AnatomySlot.Fur: // mechones
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 b = c + new Vector2(k * r * 0.5f, r * 0.6f);
                    Vector2 t = b + new Vector2(k * r * 0.15f, -r * 1.2f);
                    ci.DrawLine(b, t, col, 3f);
                }
                break;

            case AnatomySlot.Scales: // escamas (arcos)
                for (int row = 0; row < 2; row++)
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector2 p = c + new Vector2(k * r * 0.55f + row * r * 0.27f, -r * 0.35f + row * r * 0.55f);
                        ci.DrawArc(p, r * 0.32f, Mathf.Pi, Mathf.Tau, 8, col, 2.5f);
                    }
                break;

            case AnatomySlot.Wings: // dos alas
                ci.DrawColoredPolygon(new[] { c, c + new Vector2(-r, -r * 0.35f), c + new Vector2(-r * 0.5f, r * 0.1f), c + new Vector2(-r * 0.8f, r * 0.5f) }, col);
                ci.DrawColoredPolygon(new[] { c, c + new Vector2(r, -r * 0.35f), c + new Vector2(r * 0.5f, r * 0.1f), c + new Vector2(r * 0.8f, r * 0.5f) }, col);
                break;

            case AnatomySlot.Tail: // cola en espiral
                ci.DrawArc(c, r * 0.7f, -0.4f, 4.2f, 22, col, 3.5f);
                break;

            default: // Glands: glándulas (bultos)
                ci.DrawCircle(c + new Vector2(-r * 0.4f, r * 0.05f), r * 0.38f, col);
                ci.DrawCircle(c + new Vector2(r * 0.42f, -r * 0.1f), r * 0.32f, col);
                ci.DrawCircle(c + new Vector2(r * 0.05f, r * 0.45f), r * 0.28f, col);
                break;
        }
    }

    private static void Tri(CanvasItem ci, Vector2 a, Vector2 b, Vector2 c, Color col)
        => ci.DrawColoredPolygon(new[] { a, b, c }, col);
}
