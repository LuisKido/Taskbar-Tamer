namespace TaskbarTamer.Core;

/// <summary>
/// Generador de identificadores únicos y secuenciales para partes/criaturas.
/// Es determinista (no usa tiempo ni azar) y su estado (<see cref="Peek"/>) se
/// persiste en el save para que los ids no se reutilicen entre sesiones.
/// </summary>
public sealed class IdAllocator
{
    private long _next;

    public IdAllocator(long start = 1) => _next = start;

    /// <summary>Devuelve el siguiente id y avanza el contador.</summary>
    public long Next() => _next++;

    /// <summary>Próximo id que se entregará, sin consumirlo. Persiste esto en el save.</summary>
    public long Peek => _next;
}
