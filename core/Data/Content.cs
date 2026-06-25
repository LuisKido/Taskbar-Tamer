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

    // ---------- Especies (1 criatura por habilidad) ----------

    /// <summary>Orden de presentación en la colección (Guardian es la criatura inicial).</summary>
    public static readonly Archetype[] Archetypes =
    {
        Archetype.Guardian, Archetype.Bruiser, Archetype.Charger, Archetype.Leaper, Archetype.Venomous,
    };

    public static string SpeciesName(Archetype a) => a switch
    {
        Archetype.Guardian => "Mordak",
        Archetype.Bruiser => "Rendkar",
        Archetype.Charger => "Voltfang",
        Archetype.Leaper => "Skarn",
        Archetype.Venomous => "Toxia",
        _ => "?",
    };

    public static long UnlockCost(Archetype a) => a switch
    {
        Archetype.Guardian => 0,     // inicial
        Archetype.Bruiser => 600,
        Archetype.Charger => 1200,
        Archetype.Leaper => 2000,
        Archetype.Venomous => 3000,
        _ => 1000,
    };

    private static Stats InnateFor(Archetype a) => a switch
    {
        Archetype.Guardian => new Stats(40, 3, 4, 2, 0, 0, 0, 0),                              // tanque
        Archetype.Bruiser => new Stats(28, 6, 2, 3, CritChance: 600, CritDamage: 5000, 0, 0),  // pegador
        Archetype.Charger => new Stats(26, 4, 1, 6, 0, 0, 0, 0),                               // veloz
        Archetype.Leaper => new Stats(30, 4, 2, 4, 0, 0, 0, 0),                                // saltador
        Archetype.Venomous => new Stats(27, 3, 2, 3, 0, 0, 0, StatusPower: 800),               // veneno
        _ => new Stats(28, 3, 2, 3, 0, 0, 0, 0),
    };

    /// <summary>Crea la criatura de un arquetipo (sin equipo; el jugador la equipa).</summary>
    public static Creature CreateSpecies(Archetype a, IdAllocator ids)
        => new(ids.Next(), SpeciesName(a), InnateFor(a), archetype: a);

    /// <summary>Criatura inicial del jugador: el Guardian (Mordak) con un par de partes básicas.</summary>
    public static Creature StarterCreature(IdAllocator ids, GameConfig config)
    {
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Claws, Rarity.Fresh, config),
            [AnatomySlot.Shell] = PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Shell, Rarity.Fresh, config),
        };
        return new Creature(ids.Next(), SpeciesName(Archetype.Guardian), InnateFor(Archetype.Guardian),
            equipped, archetype: Archetype.Guardian);
    }

    /// <summary>Equipo rival de demostración para el reproductor de batalla.</summary>
    public static Setup RivalSetup()
    {
        // Ids altos para no colisionar con las criaturas del jugador en las vistas.
        var brute = new Creature(900_001, "Gnasher", new Stats(26, 3, 1, 2, 0, 0, 0, 0));
        var vesp = new Creature(900_002, "Vesp",
            new Stats(18, 2, 1, 3, CritChance: 300, CritDamage: 6000, 0, 0));
        return new Setup(new[] { brute }, new[] { vesp });
    }
}
