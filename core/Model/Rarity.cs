namespace TaskbarTamer.Core.Model;

/// <summary>
/// Rareza/etapa de una parte biológica, nombrada como las etapas de evolución (tema
/// Digimon). El orden ascendente es significativo: los valores enteros se usan para
/// comparaciones, escalado y fusión (3 iguales suben un tier, hasta <see cref="BioMerge"/>).
/// </summary>
public enum Rarity
{
    Fresh = 0,
    InTraining = 1,
    Rookie = 2,
    Champion = 3,
    Ultimate = 4,
    Mega = 5,
    BurstMode = 6,
    BioMerge = 7,
}
