namespace TaskbarTamer.Core.Model;

/// <summary>
/// Rareza de una parte biológica. Afecta a la magnitud de stats, número de
/// afijos y peso en las tablas de loot. El orden ascendente es significativo:
/// los valores enteros se usan para comparaciones y escalado.
/// </summary>
public enum Rarity
{
    Comun = 0,
    PocoComun = 1,
    Raro = 2,
    Epico = 3,
    Legendario = 4,
}
