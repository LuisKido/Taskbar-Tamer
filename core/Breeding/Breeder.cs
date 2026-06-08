using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Breeding;

/// <summary>
/// Herencia genética (docs/01 §4). Un campeón que alcanza el nivel máximo puede
/// <b>retirarse</b> transmitiendo uno de sus rasgos o equipamientos como un
/// <see cref="Trait"/> permanente para la siguiente generación.
/// </summary>
public static class Breeder
{
    public static bool CanRetire(Creature champion) => Leveling.IsMaxLevel(champion);

    /// <summary>
    /// Deriva un rasgo heredable a partir de una <b>parte equipada</b> del campeón:
    /// aporta una fracción (<see cref="GameConfig.InheritanceBp"/>) de las stats de la parte.
    /// </summary>
    public static Trait TraitFromPart(Creature champion, AnatomySlot slot, GameConfig config)
    {
        RequireRetirable(champion);
        if (!champion.Equipped.TryGetValue(slot, out Part? part))
            throw new ArgumentException($"El campeón no tiene parte equipada en {slot}.", nameof(slot));

        Stats bonus = part.BaseStats.ScaleBp(config.InheritanceBp);
        return new Trait($"trait-part-{part.Id}", bonus, $"{champion.Name}: {part.Family} {slot}");
    }

    /// <summary>
    /// Transmite uno de los rasgos que el campeón <b>ya tenía</b> heredados, tal cual.
    /// </summary>
    public static Trait TraitFromExistingTrait(Creature champion, string traitId)
    {
        RequireRetirable(champion);
        foreach (Trait t in champion.Traits)
            if (t.Id == traitId)
                return t;
        throw new ArgumentException($"El campeón no posee el rasgo '{traitId}'.", nameof(traitId));
    }

    /// <summary>Aplica un rasgo a una criatura, devolviendo una nueva instancia con el rasgo añadido.</summary>
    public static Creature Inherit(Creature offspring, Trait trait)
    {
        var traits = new List<Trait>(offspring.Traits) { trait };
        return offspring.With(traits: traits);
    }

    private static void RequireRetirable(Creature champion)
    {
        if (!CanRetire(champion))
            throw new InvalidOperationException(
                $"El campeón '{champion.Name}' no está al nivel máximo ({champion.Level}/{champion.MaxLevel}).");
    }
}
