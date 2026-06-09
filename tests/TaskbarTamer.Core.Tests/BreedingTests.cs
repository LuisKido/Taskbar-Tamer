using TaskbarTamer.Core;
using TaskbarTamer.Core.Breeding;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class BreedingTests
{
    private static readonly GameConfig Config = GameConfig.Default; // BaseXpToLevel 100, InheritanceBp 50%

    private static Stats S(int hp, int atk, int def, int spd) => new(hp, atk, def, spd, 0, 0, 0, 0);

    [Fact]
    public void AddXp_sube_de_nivel_y_guarda_el_resto()
    {
        var c = new Creature(1, "bestia", S(100, 10, 5, 5), level: 1, maxLevel: 30, xp: 0);

        // Nivel 1 requiere 100 XP. Damos 250 -> sube a nivel 2 (gasta 100) y nivel 3 (gasta 200)?
        // Nivel 2 requiere 200; con 150 restantes no sube. Queda nivel 2 con 150 XP.
        Creature leveled = Leveling.AddXp(c, 250, Config);

        Assert.Equal(2, leveled.Level);
        Assert.Equal(150, leveled.Xp);
    }

    [Fact]
    public void AddXp_se_topa_en_el_nivel_maximo_y_descarta_el_excedente()
    {
        var c = new Creature(1, "bestia", S(100, 10, 5, 5), level: 1, maxLevel: 2, xp: 0);

        Creature leveled = Leveling.AddXp(c, 999_999, Config);

        Assert.Equal(2, leveled.Level);
        Assert.Equal(0, leveled.Xp);
        Assert.True(Leveling.IsMaxLevel(leveled));
    }

    [Fact]
    public void No_se_puede_retirar_si_no_esta_al_maximo()
    {
        var c = new Creature(1, "bestia", S(100, 10, 5, 5), level: 5, maxLevel: 30);
        Assert.False(Breeder.CanRetire(c));
        Assert.Throws<InvalidOperationException>(() =>
            Breeder.TraitFromPart(c, AnatomySlot.Claws, Config));
    }

    [Fact]
    public void TraitFromPart_transmite_la_mitad_de_las_stats_de_la_parte()
    {
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            // Claws Fresh -> Attack 50.
            [AnatomySlot.Claws] = PartFactory.Create(7, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config),
        };
        var champion = new Creature(1, "veterano", S(100, 0, 0, 0), equipped, level: 30, maxLevel: 30);

        Trait trait = Breeder.TraitFromPart(champion, AnatomySlot.Claws, Config);

        Assert.Equal(25, trait.StatBonus.Attack); // 50 * 50%
    }

    [Fact]
    public void Heredar_un_rasgo_hace_mas_fuerte_a_la_descendencia()
    {
        // Campeón al máximo con garras potentes.
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(7, "Abisal", AnatomySlot.Claws, Rarity.Champion, Config),
        };
        var champion = new Creature(1, "veterano", S(100, 0, 0, 0), equipped, level: 30, maxLevel: 30);

        Trait legado = Breeder.TraitFromPart(champion, AnatomySlot.Claws, Config);

        var cria = new Creature(2, "cria", S(100, 10, 5, 5));
        Creature criaConLegado = Breeder.Inherit(cria, legado);

        int antes = PowerRating.Of(new Setup(new[] { cria }, Array.Empty<Creature>()), SetRegistry.Empty);
        int despues = PowerRating.Of(new Setup(new[] { criaConLegado }, Array.Empty<Creature>()), SetRegistry.Empty);

        Assert.True(despues > antes, $"antes={antes}, despues={despues}");

        // El rasgo aparece en las stats efectivas.
        ResolvedCreature resolved = StatsResolver.Resolve(criaConLegado, SetRegistry.Empty);
        Assert.True(resolved.Stats.Attack > cria.Innate.Attack);
    }
}
