using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class RarityTests
{
    [Fact]
    public void Rarezas_estan_ordenadas_de_menor_a_mayor()
    {
        Assert.True(Rarity.Fresh < Rarity.Rookie);
        Assert.True(Rarity.Ultimate < Rarity.Mega);
        Assert.True(Rarity.Mega < Rarity.BioMerge);
    }

    [Fact]
    public void Hay_ocho_tiers_de_rareza()
    {
        Assert.Equal(8, System.Enum.GetValues<Rarity>().Length);
    }

    [Fact]
    public void BioMerge_es_el_tier_maximo()
    {
        foreach (Rarity r in System.Enum.GetValues<Rarity>())
            Assert.True(r <= Rarity.BioMerge);
    }
}
