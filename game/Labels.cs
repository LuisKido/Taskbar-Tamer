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
        Core.Model.Rarity.Comun => "Común",
        Core.Model.Rarity.PocoComun => "Poco común",
        Core.Model.Rarity.Raro => "Raro",
        Core.Model.Rarity.Epico => "Épico",
        Core.Model.Rarity.Legendario => "Legendario",
        _ => r.ToString(),
    };

    public static Color RarityColor(Rarity r) => r switch
    {
        Core.Model.Rarity.Comun => new Color(0.80f, 0.80f, 0.80f),
        Core.Model.Rarity.PocoComun => new Color(0.45f, 0.95f, 0.45f),
        Core.Model.Rarity.Raro => new Color(0.40f, 0.65f, 1.00f),
        Core.Model.Rarity.Epico => new Color(0.80f, 0.45f, 1.00f),
        Core.Model.Rarity.Legendario => new Color(1.00f, 0.70f, 0.20f),
        _ => Colors.White,
    };

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
