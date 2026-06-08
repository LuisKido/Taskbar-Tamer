using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class StatsTests
{
    [Fact]
    public void Suma_componente_a_componente()
    {
        var a = new Stats(MaxHp: 100, Attack: 10, Defense: 5, Speed: 3,
            CritChance: 0, CritDamage: 0, Evasion: 0, StatusPower: 0);
        var b = new Stats(MaxHp: 50, Attack: 20, Defense: 0, Speed: 7,
            CritChance: 0, CritDamage: 0, Evasion: 0, StatusPower: 0);

        Stats sum = a + b;

        Assert.Equal(150, sum.MaxHp);
        Assert.Equal(30, sum.Attack);
        Assert.Equal(5, sum.Defense);
        Assert.Equal(10, sum.Speed);
    }

    [Fact]
    public void ScaleBp_10000_es_identidad()
    {
        var s = new Stats(MaxHp: 100, Attack: 50, Defense: 25, Speed: 10,
            CritChance: 300, CritDamage: 0, Evasion: 0, StatusPower: 0);
        Assert.Equal(s, s.ScaleBp(10000));
    }

    [Fact]
    public void ScaleBp_duplica_con_20000()
    {
        var s = new Stats(MaxHp: 100, Attack: 50, Defense: 0, Speed: 0,
            CritChance: 0, CritDamage: 0, Evasion: 0, StatusPower: 0);
        Stats doubled = s.ScaleBp(20000);
        Assert.Equal(200, doubled.MaxHp);
        Assert.Equal(100, doubled.Attack);
    }
}
