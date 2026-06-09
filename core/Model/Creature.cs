namespace TaskbarTamer.Core.Model;

/// <summary>
/// Una criatura del equipo. Sus stats efectivas resultan de combinar su base innata
/// (especie), los rasgos heredados, las partes equipadas y las sinergias de set. La
/// resolución la hace <see cref="StatsResolver"/>.
///
/// Es inmutable: la progresión y la crianza producen nuevas instancias vía <see cref="With"/>.
/// </summary>
public sealed class Creature
{
    public long Id { get; }
    public string Name { get; }

    /// <summary>Base innata de la especie.</summary>
    public Stats Innate { get; }

    /// <summary>Partes equipadas por ranura.</summary>
    public IReadOnlyDictionary<AnatomySlot, Part> Equipped { get; }

    /// <summary>Rasgos heredados de campeones retirados (Fase 4).</summary>
    public IReadOnlyList<Trait> Traits { get; }

    public int Level { get; }
    public int MaxLevel { get; }
    public long Xp { get; }

    /// <summary>Arquetipo/especie: define la habilidad de arena intrínseca.</summary>
    public Archetype Archetype { get; }

    public Creature(
        long id,
        string name,
        Stats innate,
        IReadOnlyDictionary<AnatomySlot, Part>? equipped = null,
        IReadOnlyList<Trait>? traits = null,
        int level = 1,
        int maxLevel = 30,
        long xp = 0,
        Archetype archetype = Archetype.Bruiser)
    {
        Id = id;
        Name = name;
        Innate = innate;
        Equipped = equipped ?? new Dictionary<AnatomySlot, Part>();
        Traits = traits ?? Array.Empty<Trait>();
        Level = level;
        MaxLevel = maxLevel;
        Xp = xp;
        Archetype = archetype;
    }

    /// <summary>Crea una copia cambiando solo los campos indicados (conserva el arquetipo).</summary>
    public Creature With(
        int? level = null,
        long? xp = null,
        IReadOnlyList<Trait>? traits = null,
        IReadOnlyDictionary<AnatomySlot, Part>? equipped = null) =>
        new(Id, Name, Innate, equipped ?? Equipped, traits ?? Traits, level ?? Level, MaxLevel, xp ?? Xp, Archetype);
}
