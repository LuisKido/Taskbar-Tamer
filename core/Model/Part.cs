namespace TaskbarTamer.Core.Model;

/// <summary>
/// Parte biológica: el "equipo" del juego. Se equipa en una <see cref="AnatomySlot"/>,
/// pertenece a una familia (para sinergias de set) y aporta <see cref="Stats"/>.
/// Cada instancia tiene un <see cref="Id"/> único.
/// </summary>
public sealed class Part
{
    public long Id { get; }
    public string Family { get; }
    public AnatomySlot Slot { get; }
    public Rarity Rarity { get; }
    public Stats BaseStats { get; }

    public Part(long id, string family, AnatomySlot slot, Rarity rarity, Stats baseStats)
    {
        Id = id;
        Family = family;
        Slot = slot;
        Rarity = rarity;
        BaseStats = baseStats;
    }

    /// <summary>
    /// Clase de fusión: dos partes son fusionables entre sí si comparten familia,
    /// ranura y rareza. No incluye <see cref="Id"/> ni stats.
    /// </summary>
    public PartKind Kind => new(Family, Slot, Rarity);

    public override string ToString() => $"{Family} {Slot} [{Rarity}] #{Id}";
}

/// <summary>Identidad de fusión de una parte: familia + ranura + rareza.</summary>
public readonly record struct PartKind(string Family, AnatomySlot Slot, Rarity Rarity);
