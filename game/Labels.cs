using Godot;
using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Game;

/// <summary>Nombres y colores legibles (presentación) para enums del dominio.</summary>
public static class Labels
{
    public static string Slot(AnatomySlot s) => s switch
    {
        AnatomySlot.Claws => "Garras",
        AnatomySlot.Fangs => "Colmillos",
        AnatomySlot.Stinger => "Aguijón",
        AnatomySlot.Shell => "Caparazón",
        AnatomySlot.Fur => "Pelaje",
        AnatomySlot.Scales => "Escamas",
        AnatomySlot.Wings => "Alas",
        AnatomySlot.Tail => "Cola",
        AnatomySlot.Glands => "Glándulas",
        _ => s.ToString(),
    };

    public static string Rarity(Rarity r) => r switch
    {
        Core.Model.Rarity.Fresh => "Fresh",
        Core.Model.Rarity.InTraining => "In-Training",
        Core.Model.Rarity.Rookie => "Rookie",
        Core.Model.Rarity.Champion => "Champion",
        Core.Model.Rarity.Ultimate => "Ultimate",
        Core.Model.Rarity.Mega => "Mega",
        Core.Model.Rarity.BurstMode => "Burst Mode",
        Core.Model.Rarity.BioMerge => "BioMerge",
        _ => r.ToString(),
    };

    public static Color RarityColor(Rarity r) => r switch
    {
        Core.Model.Rarity.Fresh => new Color(0.80f, 0.80f, 0.80f),       // gris
        Core.Model.Rarity.InTraining => new Color(0.45f, 0.95f, 0.45f),  // verde
        Core.Model.Rarity.Rookie => new Color(0.40f, 0.65f, 1.00f),      // azul
        Core.Model.Rarity.Champion => new Color(0.80f, 0.45f, 1.00f),    // morado
        Core.Model.Rarity.Ultimate => new Color(1.00f, 0.70f, 0.20f),    // dorado
        Core.Model.Rarity.Mega => new Color(1.00f, 0.35f, 0.35f),        // rojo
        Core.Model.Rarity.BurstMode => new Color(0.35f, 0.95f, 0.95f),   // cian
        Core.Model.Rarity.BioMerge => new Color(1.00f, 1.00f, 1.00f),    // blanco radiante
        _ => Colors.White,
    };

    /// <summary>Nombre de la habilidad de arena que la criatura obtiene según su anatomía equipada.</summary>
    public static string AbilityName(Creature c)
    {
        if (c.Equipped.ContainsKey(AnatomySlot.Shell) || c.Equipped.ContainsKey(AnatomySlot.Fur) || c.Equipped.ContainsKey(AnatomySlot.Scales))
            return "Provocar (atrae enemigos)";
        if (c.Equipped.ContainsKey(AnatomySlot.Wings))
            return "Embestida (dash + AoE)";
        if (c.Equipped.ContainsKey(AnatomySlot.Tail))
            return "Salto (brinco + golpe)";
        if (c.Equipped.ContainsKey(AnatomySlot.Glands) || c.Equipped.ContainsKey(AnatomySlot.Stinger))
            return "Estallido tóxico (AoE veneno)";
        return "Tajo (golpe en área)";
    }

    /// <summary>Resumen compacto de las stats no nulas de una parte.</summary>
    public static string PartStats(Part p)
    {
        Stats s = p.BaseStats;
        var parts = new System.Collections.Generic.List<string>();
        if (s.MaxHp > 0) parts.Add($"+{s.MaxHp} vida");
        if (s.Attack > 0) parts.Add($"+{s.Attack} atk");
        if (s.Defense > 0) parts.Add($"+{s.Defense} def");
        if (s.Speed > 0) parts.Add($"+{s.Speed} vel");
        if (s.CritChance > 0) parts.Add($"+{s.CritChance / 100}% crít");
        if (s.Evasion > 0) parts.Add($"+{s.Evasion / 100}% eva");
        return string.Join(" · ", parts);
    }
}
