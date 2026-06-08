namespace TaskbarTamer.Core.Model;

/// <summary>
/// Inventario de partes biológicas recolectadas. Gestiona el almacenamiento y la
/// <b>fusión</b>: combinar <see cref="GameConfig.FusionRequirement"/> partes idénticas
/// (misma familia + ranura + rareza) las convierte en una de la rareza superior.
/// </summary>
public sealed class Inventory
{
    private readonly List<Part> _parts = new();

    public IReadOnlyList<Part> Parts => _parts;
    public int Count => _parts.Count;

    public void Add(Part part) => _parts.Add(part);

    public void AddRange(IEnumerable<Part> parts) => _parts.AddRange(parts);

    public bool Remove(Part part) => _parts.Remove(part);

    public int CountOf(PartKind kind)
    {
        int n = 0;
        foreach (Part p in _parts)
            if (p.Kind == kind) n++;
        return n;
    }

    /// <summary>
    /// Fusiona <b>un</b> grupo del tipo indicado si hay suficientes partes y no es ya
    /// la rareza máxima. Consume las partes y añade la mejorada. Devuelve la parte
    /// resultante, o <c>null</c> si no se pudo fusionar.
    /// </summary>
    public Part? Fuse(PartKind kind, IdAllocator ids, GameConfig config)
    {
        if (kind.Rarity == Rarity.Legendario)
            return null; // tope de rareza

        var toConsume = new List<Part>(config.FusionRequirement);
        foreach (Part p in _parts)
        {
            if (p.Kind == kind)
            {
                toConsume.Add(p);
                if (toConsume.Count == config.FusionRequirement)
                    break;
            }
        }

        if (toConsume.Count < config.FusionRequirement)
            return null;

        foreach (Part p in toConsume)
            _parts.Remove(p);

        Part upgraded = PartFactory.Create(ids.Next(), kind.Family, kind.Slot, kind.Rarity + 1, config);
        _parts.Add(upgraded);
        return upgraded;
    }

    /// <summary>
    /// Fusiona automáticamente todo lo fusionable, en cascada (las partes mejoradas
    /// pueden a su vez habilitar nuevas fusiones). Devuelve el número de fusiones hechas.
    /// </summary>
    public int AutoFuse(IdAllocator ids, GameConfig config)
    {
        int fusions = 0;
        bool changed = true;
        while (changed)
        {
            changed = false;

            // Snapshot de los tipos presentes que pueden fusionar.
            var fusableKinds = new HashSet<PartKind>();
            foreach (Part p in _parts)
                if (p.Rarity != Rarity.Legendario)
                    fusableKinds.Add(p.Kind);

            foreach (PartKind kind in fusableKinds)
            {
                while (CountOf(kind) >= config.FusionRequirement)
                {
                    if (Fuse(kind, ids, config) is null)
                        break;
                    fusions++;
                    changed = true;
                }
            }
        }

        return fusions;
    }
}
