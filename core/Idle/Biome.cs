using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Idle;

/// <summary>Una entrada de la tabla de loot de un bioma, con su peso para selección ponderada.</summary>
public sealed record LootEntry(string Family, AnatomySlot Slot, Rarity Rarity, int Weight);

/// <summary>
/// Un bioma que las criaturas recorren mientras farmean. Define qué loot puede caer
/// (tabla ponderada) y el poder mínimo de equipo necesario para farmearlo.
/// </summary>
public sealed class Biome
{
    public string Id { get; }

    /// <summary>Poder de equipo mínimo para poder farmear aquí.</summary>
    public int RequiredPower { get; }

    public IReadOnlyList<LootEntry> LootTable { get; }

    /// <summary>Suma de todos los pesos; calculada una vez para la selección ponderada.</summary>
    public int TotalWeight { get; }

    public Biome(string id, int requiredPower, IReadOnlyList<LootEntry> lootTable)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id de bioma vacío.", nameof(id));
        if (lootTable.Count == 0)
            throw new ArgumentException("La tabla de loot no puede estar vacía.", nameof(lootTable));

        int total = 0;
        foreach (LootEntry entry in lootTable)
        {
            if (entry.Weight <= 0)
                throw new ArgumentException($"Peso inválido ({entry.Weight}) en {entry.Family}.", nameof(lootTable));
            total += entry.Weight;
        }

        Id = id;
        RequiredPower = requiredPower;
        LootTable = lootTable;
        TotalWeight = total;
    }
}
