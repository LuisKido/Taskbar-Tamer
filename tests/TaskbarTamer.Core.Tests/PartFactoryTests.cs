using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class PartFactoryTests
{
    private static readonly GameConfig Config = GameConfig.Default;

    [Fact]
    public void Mayor_rareza_da_mejores_stats()
    {
        Part comun = PartFactory.Create(1, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config);
        Part epico = PartFactory.Create(2, "Abisal", AnatomySlot.Claws, Rarity.Champion, Config);

        Assert.True(epico.BaseStats.Attack > comun.BaseStats.Attack);
    }

    [Fact]
    public void Ranura_ofensiva_aporta_ataque_no_vida()
    {
        Part claws = PartFactory.Create(1, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config);
        Assert.True(claws.BaseStats.Attack > 0);
        Assert.Equal(0, claws.BaseStats.MaxHp);
    }

    [Fact]
    public void Ranura_defensiva_aporta_vida_no_ataque()
    {
        Part shell = PartFactory.Create(1, "Abisal", AnatomySlot.Shell, Rarity.Fresh, Config);
        Assert.True(shell.BaseStats.MaxHp > 0);
        Assert.Equal(0, shell.BaseStats.Attack);
    }

    [Fact]
    public void Fresh_aplica_factor_x1()
    {
        Part comun = PartFactory.Create(1, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config);
        // La plantilla base de Claws es Attack 50 y Fresh = x1.0.
        Assert.Equal(50, comun.BaseStats.Attack);
    }
}
