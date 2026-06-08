namespace TaskbarTamer.Core.Model;

/// <summary>
/// Rasgo heredado: bonificación de stats <b>permanente</b> que un campeón retirado
/// transmite a la siguiente generación (ver herencia genética en docs/01 §4). Se
/// suma a la base innata de la criatura que lo recibe.
/// </summary>
/// <param name="Id">Identificador del rasgo.</param>
/// <param name="StatBonus">Bonificación de stats que aporta.</param>
/// <param name="Origin">De dónde proviene (para mostrarlo al jugador).</param>
public sealed record Trait(string Id, Stats StatBonus, string Origin);
