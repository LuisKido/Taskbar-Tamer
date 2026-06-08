using TaskbarTamer.Core.Rng;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class DeterministicRngTests
{
    [Fact]
    public void Misma_semilla_produce_la_misma_secuencia()
    {
        var a = new DeterministicRng(12345);
        var b = new DeterministicRng(12345);

        for (int i = 0; i < 100; i++)
            Assert.Equal(a.NextU64(), b.NextU64());
    }

    [Fact]
    public void Semillas_distintas_divergen()
    {
        var a = new DeterministicRng(1);
        var b = new DeterministicRng(2);
        Assert.NotEqual(a.NextU64(), b.NextU64());
    }

    [Fact]
    public void El_estado_permite_reanudar_exactamente_la_secuencia()
    {
        var rng = new DeterministicRng(999);
        rng.NextU64();
        rng.NextU64();
        ulong saved = rng.State;

        ulong expectedNext = rng.NextU64();

        // Reconstruir desde el estado guardado debe dar el mismo siguiente valor.
        var resumed = new DeterministicRng(saved);
        Assert.Equal(expectedNext, resumed.NextU64());
    }

    [Fact]
    public void NextInt_siempre_dentro_del_rango()
    {
        var rng = new DeterministicRng(42);
        for (int i = 0; i < 1000; i++)
        {
            int v = rng.NextInt(7);
            Assert.InRange(v, 0, 6);
        }
    }

    [Fact]
    public void NextInt_con_rango_no_positivo_lanza()
    {
        var rng = new DeterministicRng(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
    }
}
