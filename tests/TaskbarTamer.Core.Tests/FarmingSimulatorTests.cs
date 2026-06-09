using TaskbarTamer.Core;
using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class FarmingSimulatorTests
{
    private static readonly GameConfig Config = GameConfig.Default; // interval 60s, cap 12h

    /// <summary>Bioma simple de una sola entrada (loot determinista en su tipo).</summary>
    private static Biome SingleEntryBiome(int requiredPower = 0) => new(
        "test-mono",
        requiredPower,
        new[] { new LootEntry("Abisal", AnatomySlot.Claws, Rarity.Fresh, 1) });

    /// <summary>Bioma con dos entradas de pesos muy distintos (1 vs 99).</summary>
    private static Biome WeightedBiome() => new(
        "test-weighted",
        0,
        new[]
        {
            new LootEntry("Rookie", AnatomySlot.Fangs, Rarity.Rookie, 1),
            new LootEntry("Fresh", AnatomySlot.Claws, Rarity.Fresh, 99),
        });

    [Fact]
    public void Encuentros_igual_a_tiempo_dividido_por_intervalo()
    {
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(TeamPower: 100, RngState: 1),
            SingleEntryBiome(), elapsedSeconds: 1200, new IdAllocator(), Config);

        Assert.Equal(20, r.Encounters);           // 1200 / 60
        Assert.Equal(20, r.Loot.Count);
        Assert.Equal(200, r.XpGained);            // 20 * 10
    }

    [Fact]
    public void Tiempo_insuficiente_no_da_loot()
    {
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(100, 1),
            SingleEntryBiome(), elapsedSeconds: 59, new IdAllocator(), Config);

        Assert.Equal(0, r.Encounters);
        Assert.Empty(r.Loot);
        Assert.Equal(1UL, r.NewRngState); // RNG no avanza si no hay encuentros
    }

    [Fact]
    public void Poder_insuficiente_no_permite_farmear()
    {
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(TeamPower: 10, RngState: 1),
            SingleEntryBiome(requiredPower: 100), elapsedSeconds: 100000, new IdAllocator(), Config);

        Assert.Equal(0, r.Encounters);
        Assert.Empty(r.Loot);
    }

    [Fact]
    public void El_progreso_offline_se_topa()
    {
        var cfg = new GameConfig { EncounterIntervalSeconds = 60, OfflineCapSeconds = 600 };
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(100, 1),
            SingleEntryBiome(), elapsedSeconds: 999_999, new IdAllocator(), cfg);

        Assert.Equal(10, r.Encounters); // tope 600s / 60 = 10
    }

    [Fact]
    public void Mismo_input_produce_el_mismo_loot()
    {
        Biome biome = WeightedBiome();
        var assignment = new FarmingAssignment(100, RngState: 7);

        FarmingResult a = FarmingSimulator.Resolve(assignment, biome, 6000, new IdAllocator(), Config);
        FarmingResult b = FarmingSimulator.Resolve(assignment, biome, 6000, new IdAllocator(), Config);

        Assert.Equal(a.Loot.Count, b.Loot.Count);
        for (int i = 0; i < a.Loot.Count; i++)
            Assert.Equal(a.Loot[i].Kind, b.Loot[i].Kind);
        Assert.Equal(a.NewRngState, b.NewRngState);
    }

    [Fact]
    public void Reanudar_por_tramos_equivale_a_resolver_de_una_vez()
    {
        Biome biome = WeightedBiome();
        ulong seed = 7;

        // De una vez: 1200s = 20 encuentros.
        FarmingResult whole = FarmingSimulator.Resolve(
            new FarmingAssignment(100, seed), biome, 1200, new IdAllocator(), Config);

        // En dos tramos de 600s usando el estado de RNG devuelto.
        FarmingResult first = FarmingSimulator.Resolve(
            new FarmingAssignment(100, seed), biome, 600, new IdAllocator(), Config);
        FarmingResult second = FarmingSimulator.Resolve(
            new FarmingAssignment(100, first.NewRngState), biome, 600, new IdAllocator(), Config);

        var split = first.Loot.Concat(second.Loot).Select(p => p.Kind).ToList();
        var wholeKinds = whole.Loot.Select(p => p.Kind).ToList();

        Assert.Equal(wholeKinds, split);
        Assert.Equal(whole.NewRngState, second.NewRngState);
    }

    [Fact]
    public void La_seleccion_respeta_los_pesos()
    {
        Biome biome = WeightedBiome(); // Rookie peso 1, Fresh peso 99
        // Config sin tope restrictivo para llegar a 1000 encuentros.
        var cfg = new GameConfig { EncounterIntervalSeconds = 60, OfflineCapSeconds = 10_000_000 };
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(100, 123), biome, 60_000, new IdAllocator(), cfg); // 1000 encuentros

        int comun = r.Loot.Count(p => p.Rarity == Rarity.Fresh);
        int raro = r.Loot.Count(p => p.Rarity == Rarity.Rookie);

        Assert.Equal(1000, comun + raro);
        Assert.True(comun > raro, $"Fresh={comun}, Rookie={raro}");
    }

    [Fact]
    public void Cada_parte_recibe_un_id_unico()
    {
        FarmingResult r = FarmingSimulator.Resolve(
            new FarmingAssignment(100, 1),
            SingleEntryBiome(), elapsedSeconds: 1200, new IdAllocator(), Config);

        int distinct = r.Loot.Select(p => p.Id).Distinct().Count();
        Assert.Equal(r.Loot.Count, distinct);
    }
}
