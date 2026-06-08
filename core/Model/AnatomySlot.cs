namespace TaskbarTamer.Core.Model;

/// <summary>
/// Ranura de anatomía donde se equipa una <see cref="Part"/>. Reemplaza al equipo
/// clásico de RPG. Agrupadas en tres categorías de diseño (ver <see cref="SlotCategory"/>).
/// </summary>
public enum AnatomySlot
{
    // Ofensiva
    Claws,
    Fangs,
    Stinger,

    // Defensiva
    Shell,
    Fur,
    Scales,

    // Utilidad / Movilidad
    Wings,
    Tail,
    Glands,
}

/// <summary>Categoría de diseño de una ranura: determina qué stats aporta principalmente.</summary>
public enum SlotCategory
{
    Offense,
    Defense,
    Utility,
}

public static class AnatomySlotExtensions
{
    public static SlotCategory Category(this AnatomySlot slot) => slot switch
    {
        AnatomySlot.Claws or AnatomySlot.Fangs or AnatomySlot.Stinger => SlotCategory.Offense,
        AnatomySlot.Shell or AnatomySlot.Fur or AnatomySlot.Scales => SlotCategory.Defense,
        _ => SlotCategory.Utility,
    };
}
