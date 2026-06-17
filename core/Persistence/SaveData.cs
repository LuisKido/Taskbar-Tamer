using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Persistence;

/// <summary>
/// Estado persistible de la partida (lo que cuenta para el progreso PvE/idle). El
/// cliente Godot solo tiene que leer/escribir este objeto en <c>user://</c>; toda la
/// estructura vive en core/ para mantener el save desacoplado del motor.
///
/// Nota: el estado competitivo autoritativo (setup bloqueado, liga) vivirá en el
/// servidor, no aquí (ver docs/02 §6).
/// </summary>
public sealed class SaveData
{
    /// <summary>Versión del formato de save, para migraciones futuras.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Próximo id a entregar por el <see cref="IdAllocator"/> (continuidad de ids entre sesiones).</summary>
    public long NextId { get; set; } = 1;

    /// <summary>Estado del RNG de farming, para reanudar la secuencia sin saltos.</summary>
    public ulong FarmingRngState { get; set; }

    /// <summary>Bioma en el que el equipo está farmeando actualmente (null si ninguno).</summary>
    public string? CurrentBiomeId { get; set; }

    /// <summary>Marca de tiempo (Unix, segundos) de la última resolución de farming, para el progreso offline.</summary>
    public long LastFarmedUnixSeconds { get; set; }

    /// <summary>Esencia genética acumulada (moneda para reclutar criaturas).</summary>
    public long Essence { get; set; }

    /// <summary>Fase alcanzada en la arena (progreso de oleadas).</summary>
    public int Stage { get; set; } = 1;

    /// <summary>Escala de la interfaz (1.0 = normal). Ajuste del jugador para adaptarse a su pantalla.</summary>
    public float UiScale { get; set; } = 1f;

    /// <summary>Ventana siempre encima.</summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>Partes recolectadas.</summary>
    public List<Part> Inventory { get; set; } = new();

    /// <summary>Criaturas del jugador.</summary>
    public List<Creature> Roster { get; set; } = new();

    /// <summary>Ids de criaturas en la línea frontal (en orden). Subconjunto del roster.</summary>
    public List<long> FrontLine { get; set; } = new();

    /// <summary>Ids de criaturas en la retaguardia (en orden). Subconjunto del roster.</summary>
    public List<long> BackLine { get; set; } = new();
}
