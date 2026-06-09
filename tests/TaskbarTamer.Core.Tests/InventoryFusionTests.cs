using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class InventoryFusionTests
{
    private static readonly GameConfig Config = GameConfig.Default; // FusionRequirement = 3

    private static Part Fresh(IdAllocator ids) =>
        PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config);

    [Fact]
    public void Tres_iguales_se_fusionan_en_una_de_rareza_superior()
    {
        var ids = new IdAllocator();
        var inv = new Inventory();
        for (int i = 0; i < 3; i++) inv.Add(Fresh(ids));

        Part? result = inv.Fuse(new PartKind("Abisal", AnatomySlot.Claws, Rarity.Fresh), ids, Config);

        Assert.NotNull(result);
        Assert.Equal(Rarity.InTraining, result!.Rarity);
        Assert.Equal(1, inv.Count); // 3 consumidas, 1 creada
    }

    [Fact]
    public void No_fusiona_si_no_hay_suficientes()
    {
        var ids = new IdAllocator();
        var inv = new Inventory();
        inv.Add(Fresh(ids));
        inv.Add(Fresh(ids));

        Part? result = inv.Fuse(new PartKind("Abisal", AnatomySlot.Claws, Rarity.Fresh), ids, Config);

        Assert.Null(result);
        Assert.Equal(2, inv.Count);
    }

    [Fact]
    public void BioMerge_no_puede_fusionarse()
    {
        var ids = new IdAllocator();
        var inv = new Inventory();
        for (int i = 0; i < 3; i++)
            inv.Add(PartFactory.Create(ids.Next(), "Abisal", AnatomySlot.Claws, Rarity.BioMerge, Config));

        Part? result = inv.Fuse(new PartKind("Abisal", AnatomySlot.Claws, Rarity.BioMerge), ids, Config);

        Assert.Null(result);
        Assert.Equal(3, inv.Count);
    }

    [Fact]
    public void AutoFuse_hace_cascada_de_nueve_comunes_a_una_rara()
    {
        var ids = new IdAllocator();
        var inv = new Inventory();
        for (int i = 0; i < 9; i++) inv.Add(Fresh(ids));

        // 9 Fresh -> 3 InTraining -> 1 Rookie. Total 4 fusiones.
        int fusions = inv.AutoFuse(ids, Config);

        Assert.Equal(4, fusions);
        Assert.Equal(1, inv.Count);
        Assert.Equal(Rarity.Rookie, inv.Parts[0].Rarity);
    }

    [Fact]
    public void AutoFuse_deja_el_resto_sin_fusionar()
    {
        var ids = new IdAllocator();
        var inv = new Inventory();
        for (int i = 0; i < 4; i++) inv.Add(Fresh(ids)); // 3 fusionan, 1 sobra

        int fusions = inv.AutoFuse(ids, Config);

        Assert.Equal(1, fusions);
        Assert.Equal(2, inv.Count); // 1 InTraining + 1 Fresh sobrante
        Assert.Equal(1, inv.CountOf(new PartKind("Abisal", AnatomySlot.Claws, Rarity.Fresh)));
        Assert.Equal(1, inv.CountOf(new PartKind("Abisal", AnatomySlot.Claws, Rarity.InTraining)));
    }
}
