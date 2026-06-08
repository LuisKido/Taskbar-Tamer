namespace TaskbarTamer.Core.Rng;

/// <summary>
/// PRNG determinista (algoritmo <c>splitmix64</c>). Dado el mismo estado inicial,
/// produce siempre la misma secuencia en cualquier máquina/CPU — requisito para
/// que la simulación competitiva sea reproducible (ver docs/02 §3).
///
/// El <see cref="State"/> captura por completo la posición en la secuencia: para
/// reanudar exactamente donde se quedó, persiste <see cref="State"/> y reconstruye
/// con <c>new DeterministicRng(state)</c>.
/// </summary>
public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(ulong seed) => _state = seed;

    /// <summary>Estado actual; persistir para reanudar la secuencia sin saltos.</summary>
    public ulong State => _state;

    public ulong NextU64()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>
    /// Entero uniforme en <c>[0, maxExclusive)</c>. Para los rangos pequeños del
    /// juego (pesos de loot) el sesgo de módulo es despreciable.
    /// </summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Debe ser > 0.");
        return (int)(NextU64() % (ulong)maxExclusive);
    }
}
