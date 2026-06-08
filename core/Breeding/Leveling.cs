using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Breeding;

/// <summary>
/// Progresión de nivel de una criatura. La XP para subir del nivel N es
/// <c>BaseXpToLevel * N</c>. Al llegar a <see cref="Creature.MaxLevel"/> la criatura
/// deja de subir (la XP excedente se descarta) y queda lista para retirarse.
/// </summary>
public static class Leveling
{
    public static bool IsMaxLevel(Creature creature) => creature.Level >= creature.MaxLevel;

    public static long XpToNext(int level, GameConfig config) => config.BaseXpToLevel * level;

    /// <summary>Añade XP y devuelve una nueva criatura con el nivel/XP resultantes.</summary>
    public static Creature AddXp(Creature creature, long xp, GameConfig config)
    {
        if (xp < 0)
            throw new ArgumentOutOfRangeException(nameof(xp), "La XP no puede ser negativa.");

        int level = creature.Level;
        long pool = creature.Xp + xp;

        while (level < creature.MaxLevel)
        {
            long need = XpToNext(level, config);
            if (pool < need)
                break;
            pool -= need;
            level++;
        }

        if (level >= creature.MaxLevel)
        {
            level = creature.MaxLevel;
            pool = 0; // tope de nivel: no se acumula XP sobrante
        }

        return creature.With(level: level, xp: pool);
    }
}
