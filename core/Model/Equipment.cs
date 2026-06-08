namespace TaskbarTamer.Core.Model;

/// <summary>
/// Operaciones puras de equipar/desequipar partes en una criatura. Como
/// <see cref="Creature"/> es inmutable, devuelven una nueva instancia junto con la
/// parte desplazada/retirada (para devolverla al inventario).
/// </summary>
public static class Equipment
{
    /// <summary>
    /// Equipa <paramref name="part"/> en su ranura (<see cref="Part.Slot"/>). Si la
    /// ranura ya tenía una parte, se devuelve como <c>displaced</c>.
    /// </summary>
    public static (Creature creature, Part? displaced) Equip(Creature creature, Part part)
    {
        var dict = new Dictionary<AnatomySlot, Part>(creature.Equipped);
        dict.TryGetValue(part.Slot, out Part? displaced);
        dict[part.Slot] = part;
        return (creature.With(equipped: dict), displaced);
    }

    /// <summary>
    /// Retira la parte de una ranura. Devuelve la criatura sin esa parte y la parte
    /// retirada (o <c>null</c> si la ranura estaba vacía).
    /// </summary>
    public static (Creature creature, Part? removed) Unequip(Creature creature, AnatomySlot slot)
    {
        if (!creature.Equipped.TryGetValue(slot, out Part? removed))
            return (creature, null);

        var dict = new Dictionary<AnatomySlot, Part>(creature.Equipped);
        dict.Remove(slot);
        return (creature.With(equipped: dict), removed);
    }
}
