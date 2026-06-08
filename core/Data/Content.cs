using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;

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
            new LootEntry("Abisal", AnatomySlot.Fangs, Rarity.Comun, 70),
            new LootEntry("Abisal", AnatomySlot.Claws, Rarity.Raro, 25),
            new LootEntry("Abisal", AnatomySlot.Stinger, Rarity.Epico, 5),
        });

    /// <summary>Criatura inicial del jugador, con un par de partes básicas equipadas.</summary>
    public static Creature StarterCreature(IdAllocator ids, GameConfig config)
    {
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Claws, Rarity.Comun, config),
            [AnatomySlot.Shell] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Shell, Rarity.Comun, config),
        };
        return new Creature(ids.Next(), "Mordak", new Stats(300, 20, 10, 15, 0, 0, 0, 0), equipped);
    }
}
