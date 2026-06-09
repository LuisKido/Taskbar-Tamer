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
        Archetype.Guardian => new Stats(360, 16, 18, 10, 0, 0, 0, 0),                                   // tanque
        Archetype.Bruiser => new Stats(240, 30, 8, 14, CritChance: 1500, CritDamage: 5000, 0, 0),       // pegador
        Archetype.Charger => new Stats(220, 22, 6, 24, 0, 0, 0, 0),                                     // veloz
        Archetype.Leaper => new Stats(260, 20, 10, 18, 0, 0, 0, 0),                                     // saltador
        Archetype.Venomous => new Stats(230, 18, 8, 14, 0, 0, 0, StatusPower: 2000),                    // veneno
        _ => new Stats(250, 18, 10, 14, 0, 0, 0, 0),
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
        var brute = new Creature(900_001, "Gnasher", new Stats(260, 22, 8, 12, 0, 0, 0, 0));
        var vesp = new Creature(900_002, "Vesp",
            new Stats(170, 16, 4, 24, CritChance: 2500, CritDamage: 6000, 0, 0));
        return new Setup(new[] { brute }, new[] { vesp });
    }
}
