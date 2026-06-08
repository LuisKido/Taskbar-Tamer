namespace TaskbarTamer.Core.Model;

/// <summary>
/// Una criatura del equipo. Sus stats efectivas resultan de combinar su base innata
/// (especie + rasgos heredados), las partes equipadas y las sinergias de set. La
/// resolución la hace <see cref="StatsResolver"/>.
/// </summary>
public sealed class Creature
{
    public long Id { get; }
    public string Name { get; }

    /// <summary>Base innata: stats de especie + rasgos heredados (Fase 4).</summary>
    public Stats Innate { get; }

    /// <summary>Partes equipadas por ranura.</summary>
    public IReadOnlyDictionary<AnatomySlot, Part> Equipped { get; }

    public Creature(
        long id,
        string name,
        Stats innate,
        IReadOnlyDictionary<AnatomySlot, Part>? equipped = null)
    {
        Id = id;
        Name = name;
        Innate = innate;
        Equipped = equipped ?? new Dictionary<AnatomySlot, Part>();
    }
}
