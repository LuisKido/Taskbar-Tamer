namespace TaskbarTamer.Core.Simulation;

/// <summary>Desenlace de una batalla.</summary>
public enum BattleOutcome
{
    TeamA,
    TeamB,
    Draw,
}

/// <summary>Tipo de evento ocurrido durante la batalla (para reproducir con animaciones).</summary>
public enum BattleEventType
{
    Attack,
    Evade,
    PoisonTick,
    Death,
}

/// <summary>
/// Un evento de batalla. El cliente <b>reproduce</b> esta secuencia con animaciones;
/// no recalcula la batalla (ver docs/02 §3).
/// </summary>
/// <param name="Round">Ronda en la que ocurre (desde 1).</param>
/// <param name="Type">Qué ocurrió.</param>
/// <param name="ActorId">Quién origina el evento (para PoisonTick/Death = la propia criatura).</param>
/// <param name="TargetId">Sobre quién recae.</param>
/// <param name="Value">Daño aplicado (0 si no aplica).</param>
/// <param name="Crit">Si el ataque fue crítico.</param>
public sealed record BattleEvent(
    int Round,
    BattleEventType Type,
    long ActorId,
    long TargetId,
    int Value,
    bool Crit);

/// <summary>Resultado completo de una batalla: desenlace, rondas jugadas y log reproducible.</summary>
public sealed record BattleResult(
    BattleOutcome Outcome,
    int Rounds,
    IReadOnlyList<BattleEvent> Log);
