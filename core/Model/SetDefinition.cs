namespace TaskbarTamer.Core.Model;

/// <summary>
/// Bonificación de un set al alcanzar cierto número de piezas equipadas de la misma
/// familia. Los umbrales son acumulativos: con 4 piezas se aplican los umbrales de
/// 2, 3 y 4 que existan.
/// </summary>
public sealed record SetThreshold(int Pieces, Stats Bonus, IReadOnlyList<CombatKeyword> Keywords)
{
    public static SetThreshold Of(int pieces, Stats bonus, params CombatKeyword[] keywords) =>
        new(pieces, bonus, keywords);
}

/// <summary>Definición de una familia de partes y sus bonificaciones de set por umbral.</summary>
public sealed class SetDefinition
{
    public string Family { get; }
    public IReadOnlyList<SetThreshold> Thresholds { get; }

    public SetDefinition(string family, IReadOnlyList<SetThreshold> thresholds)
    {
        if (string.IsNullOrWhiteSpace(family))
            throw new ArgumentException("Familia vacía.", nameof(family));
        Family = family;
        Thresholds = thresholds;
    }
}

/// <summary>Catálogo de definiciones de set, indexado por familia.</summary>
public sealed class SetRegistry
{
    private readonly Dictionary<string, SetDefinition> _byFamily;

    public SetRegistry(IEnumerable<SetDefinition> definitions)
    {
        _byFamily = new Dictionary<string, SetDefinition>();
        foreach (SetDefinition def in definitions)
            _byFamily[def.Family] = def;
    }

    public SetDefinition? Find(string family) =>
        _byFamily.TryGetValue(family, out SetDefinition? def) ? def : null;

    /// <summary>Registro sin ningún set definido.</summary>
    public static SetRegistry Empty { get; } = new(Array.Empty<SetDefinition>());
}
