using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Persistence;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class SaveSerializerTests
{
    private static readonly GameConfig Config = GameConfig.Default;

    private static SaveData SampleSave()
    {
        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(10, "Abisal", AnatomySlot.Claws, Rarity.Rookie, Config),
            [AnatomySlot.Shell] = PartFactory.Create(11, "Abisal", AnatomySlot.Shell, Rarity.Fresh, Config),
        };
        var traits = new[] { new Trait("t-1", new Stats(0, 25, 0, 0, 0, 0, 0, 0), "veterano: Abisal Claws") };
        var creature = new Creature(1, "alfa", new Stats(120, 10, 8, 6, 0, 0, 0, 0),
            equipped, traits, level: 12, maxLevel: 30, xp: 340);

        return new SaveData
        {
            NextId = 42,
            FarmingRngState = 0xDEADBEEFCAFEUL,
            CurrentBiomeId = "bosque-abisal",
            LastFarmedUnixSeconds = 1_700_000_000,
            Inventory = { PartFactory.Create(20, "Volcanica", AnatomySlot.Fangs, Rarity.Champion, Config) },
            Roster = { creature },
        };
    }

    [Fact]
    public void Round_trip_conserva_el_estado()
    {
        SaveData original = SampleSave();

        string json = SaveSerializer.Serialize(original);
        SaveData restored = SaveSerializer.Deserialize(json);

        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.NextId, restored.NextId);
        Assert.Equal(original.FarmingRngState, restored.FarmingRngState);
        Assert.Equal(original.CurrentBiomeId, restored.CurrentBiomeId);
        Assert.Equal(original.LastFarmedUnixSeconds, restored.LastFarmedUnixSeconds);

        Assert.Single(restored.Inventory);
        Assert.Equal(20, restored.Inventory[0].Id);
        Assert.Equal(Rarity.Champion, restored.Inventory[0].Rarity);
    }

    [Fact]
    public void Round_trip_conserva_criaturas_partes_equipadas_y_rasgos()
    {
        SaveData restored = SaveSerializer.Deserialize(SaveSerializer.Serialize(SampleSave()));

        Creature c = Assert.Single(restored.Roster);
        Assert.Equal("alfa", c.Name);
        Assert.Equal(12, c.Level);
        Assert.Equal(340, c.Xp);

        Assert.Equal(2, c.Equipped.Count);
        Assert.True(c.Equipped.ContainsKey(AnatomySlot.Claws));
        Assert.Equal("Abisal", c.Equipped[AnatomySlot.Claws].Family);

        Trait trait = Assert.Single(c.Traits);
        Assert.Equal(25, trait.StatBonus.Attack);
    }

    [Fact]
    public void Las_enumeraciones_se_serializan_como_texto()
    {
        string json = SaveSerializer.Serialize(SampleSave());
        Assert.Contains("Champion", json);   // rareza como texto, no como número
        Assert.Contains("Claws", json);   // ranura como texto
    }
}
