using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Core.Data;

/// <summary>
/// Contenido del juego (biomas, criatura inicial…). Por ahora hardcodeado; el destino
/// es cargarlo desde JSON en core/Data. Compartido por cliente y servidor.
/// </summary>
public static class Content
{
    public const string DefaultBiomeId = "Bosque Abisal";

    public static Biome DefaultBiome() => new(
        DefaultBiomeId,
        requiredPower: 0,
        new[]
        {
            new LootEntry("Abisal", AnatomySlot.Fangs, Rarity.Fresh, 70),
            new LootEntry("Abisal", AnatomySlot.Claws, Rarity.Rookie, 25),
            new LootEntry("Abisal", AnatomySlot.Stinger, Rarity.Champion, 5),
        });

    /// <summary>Criatura inicial del jugador, con un par de partes básicas equipadas.</summary>
    public static Creature StarterCreature(IdAllocator ids, GameConfig config)
    {
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Claws, Rarity.Fresh, config),
            [AnatomySlot.Shell] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Shell, Rarity.Fresh, config),
        };
        return new Creature(ids.Next(), "Mordak", new Stats(300, 20, 10, 15, 0, 0, 0, 0), equipped);
    }

    /// <summary>Equipo rival de demostración para el reproductor de batalla.</summary>
    public static Setup RivalSetup()
    {
        // Ids altos para no colisionar con las criaturas del jugador en las vistas.
        var brute = new Creature(900_001, "Gnasher", new Stats(260, 22, 8, 12, 0, 0, 0, 0));
        var vesp = new Creature(900_002, "Vesp",
            new Stats(170, 16, 4, 24, CritChance: 2500, CritDamage: 6000, 0, 0));
        return new Setup(new[] { brute }, new[] { vesp });
    }
}
