namespace TaskbarTamer.Core.Model;

/// <summary>
/// Construye <see cref="Part"/>s aplicando una plantilla de stats por categoría de
/// ranura, escalada por el factor de rareza de <see cref="GameConfig"/>. Es pura y
/// determinista: misma entrada → mismas stats.
/// </summary>
public static class PartFactory
{
    /// <summary>
    /// Plantilla base de stats de una ranura, <b>antes</b> de escalar por rareza.
    /// Ofensiva → daño/crítico; Defensiva → vida/defensa; Utilidad → velocidad/evasión.
    /// </summary>
    public static Stats BaseTemplate(AnatomySlot slot) => slot.Category() switch
    {
        SlotCategory.Offense => new Stats(
            MaxHp: 0, Attack: 5, Defense: 0, Speed: 0,
            CritChance: 120, CritDamage: 0, Evasion: 0, StatusPower: 0),

        SlotCategory.Defense => new Stats(
            MaxHp: 20, Attack: 0, Defense: 4, Speed: 0,
            CritChance: 0, CritDamage: 0, Evasion: 0, StatusPower: 0),

        _ => new Stats(
            MaxHp: 0, Attack: 0, Defense: 0, Speed: 3,
            CritChance: 0, CritDamage: 0, Evasion: 120, StatusPower: 0),
    };

    public static Part Create(long id, string family, AnatomySlot slot, Rarity rarity, GameConfig config)
    {
        int bp = config.RarityStatBp[rarity];
        Stats stats = BaseTemplate(slot).ScaleBp(bp);
        return new Part(id, family, slot, rarity, stats);
    }
}
