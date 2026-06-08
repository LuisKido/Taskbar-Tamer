using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core;

/// <summary>
/// Constantes de balance ajustables. Centraliza los "números mágicos" del juego
/// para poder afinarlos (y, en el futuro, cargarlos desde JSON en core/Data).
/// </summary>
public sealed class GameConfig
{
    /// <summary>Segundos entre cada encuentro de farming (1 encuentro = 1 tirada de loot).</summary>
    public int EncounterIntervalSeconds { get; init; } = 60;

    /// <summary>Tope de progreso offline acumulable (por defecto 12 h).</summary>
    public long OfflineCapSeconds { get; init; } = 12 * 3600;

    /// <summary>XP otorgada por cada encuentro.</summary>
    public long XpPerEncounter { get; init; } = 10;

    /// <summary>Partes idénticas necesarias para fusionar y subir una rareza.</summary>
    public int FusionRequirement { get; init; } = 3;

    /// <summary>Factor de escalado de stats por rareza, en basis points (10000 = x1.0).</summary>
    public IReadOnlyDictionary<Rarity, int> RarityStatBp { get; init; } = DefaultRarityBp;

    // ---------- Combate (Fase 1) ----------

    /// <summary>
    /// Constante de mitigación por defensa. Daño = atk * K / (K + def). Mayor K =
    /// la defensa importa menos. Con K=100, def=100 reduce el daño a la mitad.
    /// </summary>
    public int DefenseConstant { get; init; } = 100;

    /// <summary>Multiplicador base de crítico en basis points (15000 = x1.5). Se le suma el CritDamage del atacante.</summary>
    public int CritBaseBp { get; init; } = 15000;

    /// <summary>Tope de rondas de una batalla; si se alcanza, se decide por HP restante.</summary>
    public int MaxRounds { get; init; } = 50;

    /// <summary>Daño de veneno por stack en cada tick (inicio de ronda).</summary>
    public int PoisonDamagePerStack { get; init; } = 15;

    /// <summary>Rondas que dura el veneno tras el último golpe que lo aplica.</summary>
    public int PoisonDuration { get; init; } = 3;

    /// <summary>Stacks de veneno aplicados por golpe con la palabra clave ApplyPoisonOnHit.</summary>
    public int PoisonStacksPerHit { get; init; } = 1;

    // ---------- Progresión y crianza (Fase 4) ----------

    /// <summary>XP base por nivel; la XP para subir del nivel N es BaseXpToLevel * N.</summary>
    public long BaseXpToLevel { get; init; } = 100;

    /// <summary>
    /// Fracción (basis points) de las stats de la parte/rasgo de un campeón retirado
    /// que se transmite como rasgo permanente a la siguiente generación (5000 = 50%).
    /// </summary>
    public int InheritanceBp { get; init; } = 5000;

    public static readonly IReadOnlyDictionary<Rarity, int> DefaultRarityBp =
        new Dictionary<Rarity, int>
        {
            [Rarity.Comun] = 10000,      // x1.0
            [Rarity.PocoComun] = 13000,  // x1.3
            [Rarity.Raro] = 17000,       // x1.7
            [Rarity.Epico] = 22000,      // x2.2
            [Rarity.Legendario] = 30000, // x3.0
        };

    /// <summary>Configuración por defecto, lista para usar.</summary>
    public static GameConfig Default { get; } = new();
}
