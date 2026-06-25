using TaskbarTamer.Core.Data;
using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class CreatureFactoryTests
{
    [Fact]
    public void Misma_semilla_produce_la_misma_criatura()
    {
        Creature a = CreatureFactory.Roll(1, seed: 42);
        Creature b = CreatureFactory.Roll(1, seed: 42);

        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Innate, b.Innate);
    }

    [Fact]
    public void Semillas_distintas_pueden_variar()
    {
        Creature a = CreatureFactory.Roll(1, seed: 1);
        Creature b = CreatureFactory.Roll(2, seed: 9999);

        // Muy improbable que coincidan en todo; basta con que el sistema produzca variedad.
        Assert.True(a.Name != b.Name || a.Innate != b.Innate);
    }

    [Fact]
    public void Stats_dentro_de_los_rangos_de_diseno()
    {
        for (ulong seed = 0; seed < 200; seed++)
        {
            Stats s = CreatureFactory.Roll(1, seed).Innate;
            Assert.InRange(s.MaxHp, 30, 60);
            Assert.InRange(s.Attack, 3, 7);
            Assert.InRange(s.Defense, 1, 4);
            Assert.InRange(s.Speed, 2, 6);
        }
    }
}
