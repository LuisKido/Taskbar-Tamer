using TaskbarTamer.Core.Model;
using Xunit;

namespace TaskbarTamer.Core.Tests;

/// <summary>
/// Smoke test de la Fase 0: prueba que core/ compila, que la referencia de
/// proyecto funciona y que el pipeline de xUnit corre. Se reemplazará por
/// tests reales a medida que crezca el modelo.
/// </summary>
public class RarityTests
{
    [Fact]
    public void Rarezas_estan_ordenadas_de_menor_a_mayor()
    {
        Assert.True(Rarity.Comun < Rarity.Raro);
        Assert.True(Rarity.Raro < Rarity.Legendario);
    }

    [Fact]
    public void Hay_cinco_niveles_de_rareza()
    {
        Assert.Equal(5, System.Enum.GetValues<Rarity>().Length);
    }
}
