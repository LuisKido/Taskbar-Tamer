using Godot;
using System.Collections.Generic;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>
/// Carga los frames de una criatura desde <c>res://assets/creatures/&lt;nombre&gt;/N.png</c>
/// (cortes limpios producidos por scripts/slice_sprite.py). Los reescala al tamaño pedido.
/// Si no hay frames, devuelve lista vacía → la arena usa el sprite generado por código.
///
/// Convención: el frame base MIRA A LA DERECHA; el motor lo voltea para mirar a la izquierda.
/// Frames 0 y 1 = ciclo de idle.
/// </summary>
public static class CreatureArt
{
    private static readonly Dictionary<Archetype, string> Files = new()
    {
        [Archetype.Guardian] = "mordak",
        [Archetype.Bruiser] = "rendkar",
        [Archetype.Charger] = "voltfang",
        [Archetype.Leaper] = "skarn",
        [Archetype.Venomous] = "toxia",
    };

    private static readonly string[] MinionNames = { "minion_bosque", "minion_magma", "minion_tundra" };
    private static readonly string[] BossNames = { "boss_abisal", "boss_magma", "boss_glacial" };

    /// <summary>Sprite (frame 0) del enemigo del mapa indicado, o null si no hay asset.</summary>
    public static ImageTexture? EnemyTexture(int mapIndex, bool boss, int width)
    {
        if (mapIndex < 0 || mapIndex >= MinionNames.Length)
            return null;
        string name = boss ? BossNames[mapIndex] : MinionNames[mapIndex];
        string p = $"res://assets/enemies/{name}/0.png";
        if (!ResourceLoader.Exists(p))
            return null;

        var tex = GD.Load<Texture2D>(p);
        Image img = tex.GetImage();
        if (img.GetFormat() != Image.Format.Rgba8)
            img.Convert(Image.Format.Rgba8);
        int h = Mathf.Max(1, Mathf.RoundToInt(width * (float)img.GetHeight() / img.GetWidth()));
        img.Resize(width, h, Image.Interpolation.Lanczos);
        return ImageTexture.CreateFromImage(img);
    }

    public static List<ImageTexture> Frames(Archetype a, int width)
    {
        var list = new List<ImageTexture>();
        if (!Files.TryGetValue(a, out string? name))
            return list;

        for (int i = 0; ; i++)
        {
            string p = $"res://assets/creatures/{name}/{i}.png";
            if (!ResourceLoader.Exists(p))
                break;

            var tex = GD.Load<Texture2D>(p);
            Image img = tex.GetImage();
            if (img.GetFormat() != Image.Format.Rgba8)
                img.Convert(Image.Format.Rgba8);

            int h = Mathf.Max(1, Mathf.RoundToInt(width * (float)img.GetHeight() / img.GetWidth()));
            img.Resize(width, h, Image.Interpolation.Lanczos);
            list.Add(ImageTexture.CreateFromImage(img));
        }
        return list;
    }
}
