using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Rng;

namespace TaskbarTamer.Core.Idle;

/// <summary>Equipo asignado a farmear: su poder agregado y el estado de su RNG.</summary>
public sealed record FarmingAssignment(int TeamPower, ulong RngState);

/// <summary>Resultado de resolver un periodo de farming.</summary>
public sealed record FarmingResult(
    IReadOnlyList<Part> Loot,
    long XpGained,
    int Encounters,
    ulong NewRngState);

/// <summary>
/// Resuelve el farming idle <b>por acumulación de tiempo</b>, no simulando combate
/// frame a frame (ver docs/02 §4). Es una función pura: mismo
/// (assignment, biome, elapsedSeconds, config) → mismo resultado.
/// </summary>
public static class FarmingSimulator
{
    public static FarmingResult Resolve(
        FarmingAssignment assignment,
        Biome biome,
        long elapsedSeconds,
        IdAllocator ids,
        GameConfig config)
    {
        if (elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "No puede ser negativo.");

        // El equipo no puede farmear un bioma para el que no tiene poder suficiente.
        if (assignment.TeamPower < biome.RequiredPower)
            return Empty(assignment.RngState);

        long capped = Math.Min(elapsedSeconds, config.OfflineCapSeconds);
        int encounters = (int)(capped / config.EncounterIntervalSeconds);
        if (encounters <= 0)
            return Empty(assignment.RngState);

        var rng = new DeterministicRng(assignment.RngState);
        var loot = new List<Part>(encounters);
        for (int i = 0; i < encounters; i++)
        {
            LootEntry entry = RollLoot(biome, rng);
            loot.Add(PartFactory.Create(ids.Next(), entry.Family, entry.Slot, entry.Rarity, config));
        }

        long xp = encounters * config.XpPerEncounter;
        return new FarmingResult(loot, xp, encounters, rng.State);
    }

    private static FarmingResult Empty(ulong rngState) =>
        new(Array.Empty<Part>(), 0, 0, rngState);

    /// <summary>Selección ponderada por peso sobre la tabla de loot del bioma.</summary>
    private static LootEntry RollLoot(Biome biome, DeterministicRng rng)
    {
        int roll = rng.NextInt(biome.TotalWeight);
        foreach (LootEntry entry in biome.LootTable)
        {
            if (roll < entry.Weight)
                return entry;
            roll -= entry.Weight;
        }

        // Inalcanzable: los pesos suman TotalWeight y roll < TotalWeight. Defensivo.
        return biome.LootTable[biome.LootTable.Count - 1];
    }
}
