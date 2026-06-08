using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class EquipmentTests
{
    private static readonly GameConfig Config = GameConfig.Default;

    private static Part Claws(long id, Rarity r) =>
        PartFactory.Create(id, "Abisal", AnatomySlot.Claws, r, Config);

    private static Creature Bare() =>
        new(1, "bestia", new Stats(100, 0, 0, 0, 0, 0, 0, 0));

    [Fact]
    public void Equipar_en_ranura_vacia_no_desplaza_nada()
    {
        var (creature, displaced) = Equipment.Equip(Bare(), Claws(10, Rarity.Comun));

        Assert.Null(displaced);
        Assert.True(creature.Equipped.ContainsKey(AnatomySlot.Claws));
        Assert.Equal(10, creature.Equipped[AnatomySlot.Claws].Id);
    }

    [Fact]
    public void Equipar_en_ranura_ocupada_desplaza_la_anterior()
    {
        var (withFirst, _) = Equipment.Equip(Bare(), Claws(10, Rarity.Comun));
        var (withSecond, displaced) = Equipment.Equip(withFirst, Claws(20, Rarity.Raro));

        Assert.NotNull(displaced);
        Assert.Equal(10, displaced!.Id);                       // la vieja vuelve
        Assert.Equal(20, withSecond.Equipped[AnatomySlot.Claws].Id); // la nueva queda
    }

    [Fact]
    public void Desequipar_retira_y_devuelve_la_parte()
    {
        var (equipped, _) = Equipment.Equip(Bare(), Claws(10, Rarity.Comun));
        var (bare, removed) = Equipment.Unequip(equipped, AnatomySlot.Claws);

        Assert.NotNull(removed);
        Assert.Equal(10, removed!.Id);
        Assert.False(bare.Equipped.ContainsKey(AnatomySlot.Claws));
    }

    [Fact]
    public void Desequipar_una_ranura_vacia_no_hace_nada()
    {
        var (creature, removed) = Equipment.Unequip(Bare(), AnatomySlot.Tail);
        Assert.Null(removed);
        Assert.Empty(creature.Equipped);
    }

    [Fact]
    public void Equipar_mejora_el_poder_efectivo()
    {
        Creature bare = Bare();
        var (equipped, _) = Equipment.Equip(bare, Claws(10, Rarity.Epico));

        Stats before = StatsResolver.Resolve(bare, SetRegistry.Empty).Stats;
        Stats after = StatsResolver.Resolve(equipped, SetRegistry.Empty).Stats;

        Assert.True(after.Attack > before.Attack);
    }
}
