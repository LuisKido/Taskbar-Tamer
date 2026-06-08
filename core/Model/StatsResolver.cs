namespace TaskbarTamer.Core.Model;

/// <summary>Stats efectivas de una criatura más las palabras clave de combate que posee.</summary>
public readonly record struct ResolvedCreature(Stats Stats, IReadOnlyCollection<CombatKeyword> Keywords);

/// <summary>
/// Calcula las stats efectivas de una criatura: base innata + partes equipadas +
/// bonificaciones de set. El resultado es independiente del orden de iteración (la
/// suma es conmutativa y las keywords van a un conjunto), por lo que es determinista.
/// </summary>
public static class StatsResolver
{
    public static ResolvedCreature Resolve(Creature creature, SetRegistry sets)
    {
        Stats total = creature.Innate;

        // Rasgos heredados (Fase 4): bonificación de base permanente.
        foreach (Trait trait in creature.Traits)
            total += trait.StatBonus;

        var familyCounts = new Dictionary<string, int>();
        foreach (Part part in creature.Equipped.Values)
        {
            total += part.BaseStats;
            familyCounts[part.Family] = familyCounts.GetValueOrDefault(part.Family) + 1;
        }

        var keywords = new HashSet<CombatKeyword>();
        foreach ((string family, int count) in familyCounts)
        {
            SetDefinition? def = sets.Find(family);
            if (def is null)
                continue;

            foreach (SetThreshold threshold in def.Thresholds)
            {
                if (count < threshold.Pieces)
                    continue;

                total += threshold.Bonus;
                foreach (CombatKeyword kw in threshold.Keywords)
                    keywords.Add(kw);
            }
        }

        return new ResolvedCreature(total, keywords);
    }
}
