using TaskbarTamer.Core;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;
using Xunit;

namespace TaskbarTamer.Core.Tests;

public class BattleSimulatorTests
{
    private static readonly GameConfig Config = GameConfig.Default;

    // Helper de stats con valores por defecto para no repetir 8 argumentos.
    private static Stats S(int hp, int atk, int def, int spd,
        int crit = 0, int critDmg = 0, int eva = 0, int sp = 0) =>
        new(hp, atk, def, spd, crit, critDmg, eva, sp);

    private static Creature Mk(long id, Stats stats,
        IReadOnlyDictionary<AnatomySlot, Part>? equipped = null) =>
        new(id, $"c{id}", stats, equipped);

    private static Setup Solo(Creature c) => new(new[] { c }, Array.Empty<Creature>());

    [Fact]
    public void Mismo_input_produce_log_y_desenlace_identicos()
    {
        // Criaturas con crítico y evasión para ejercitar el RNG.
        Setup a = Solo(Mk(1, S(hp: 300, atk: 60, def: 10, spd: 50, crit: 4000, critDmg: 8000)));
        Setup b = Solo(Mk(2, S(hp: 300, atk: 40, def: 20, spd: 30, eva: 3000)));

        BattleResult r1 = BattleSimulator.Simulate(a, b, seed: 777, SetRegistry.Empty, Config);
        BattleResult r2 = BattleSimulator.Simulate(a, b, seed: 777, SetRegistry.Empty, Config);

        Assert.Equal(r1.Outcome, r2.Outcome);
        Assert.Equal(r1.Rounds, r2.Rounds);
        Assert.Equal(r1.Log, r2.Log); // BattleEvent es record → igualdad estructural
    }

    [Fact]
    public void Semillas_distintas_pueden_dar_logs_distintos()
    {
        Setup a = Solo(Mk(1, S(300, 60, 10, 50, crit: 5000, critDmg: 8000)));
        Setup b = Solo(Mk(2, S(300, 40, 20, 30, eva: 5000)));

        BattleResult r1 = BattleSimulator.Simulate(a, b, seed: 1, SetRegistry.Empty, Config);
        BattleResult r2 = BattleSimulator.Simulate(a, b, seed: 2, SetRegistry.Empty, Config);

        Assert.NotEqual(r1.Log, r2.Log);
    }

    [Fact]
    public void El_equipo_mas_fuerte_gana()
    {
        Setup fuerte = Solo(Mk(1, S(hp: 500, atk: 120, def: 30, spd: 40)));
        Setup debil = Solo(Mk(2, S(hp: 200, atk: 20, def: 5, spd: 20)));

        BattleResult r = BattleSimulator.Simulate(fuerte, debil, seed: 5, SetRegistry.Empty, Config);

        Assert.Equal(BattleOutcome.TeamA, r.Outcome);
    }

    [Fact]
    public void La_formula_de_dano_aplica_mitigacion_por_defensa()
    {
        // atk 100, def 100, K=100 -> 100 * 100 / (100+100) = 50. Sin crit (crit 0).
        Setup a = Solo(Mk(1, S(hp: 1000, atk: 100, def: 0, spd: 50)));
        Setup b = Solo(Mk(2, S(hp: 1000, atk: 0, def: 100, spd: 10)));

        BattleResult r = BattleSimulator.Simulate(a, b, seed: 1, SetRegistry.Empty, Config);

        BattleEvent firstHit = r.Log.First(e =>
            e.Type == BattleEventType.Attack && e.ActorId == 1 && e.TargetId == 2);
        Assert.Equal(50, firstHit.Value);
        Assert.False(firstHit.Crit);
    }

    [Fact]
    public void Crit_garantizado_marca_el_golpe_como_critico()
    {
        Setup a = Solo(Mk(1, S(hp: 1000, atk: 100, def: 0, spd: 50, crit: 10000, critDmg: 5000)));
        Setup b = Solo(Mk(2, S(hp: 1000, atk: 0, def: 0, spd: 10)));

        BattleResult r = BattleSimulator.Simulate(a, b, seed: 1, SetRegistry.Empty, Config);

        BattleEvent firstHit = r.Log.First(e => e.Type == BattleEventType.Attack && e.ActorId == 1);
        Assert.True(firstHit.Crit);
        // atk 100, def 0 -> 100 base; crit x(15000+5000)/10000 = x2 -> 200.
        Assert.Equal(200, firstHit.Value);
    }

    [Fact]
    public void Evasion_total_evita_todo_el_dano()
    {
        Setup atacante = Solo(Mk(1, S(hp: 1000, atk: 100, def: 0, spd: 50)));
        // Objetivo con 100% evasión y algo de ataque para no bloquearse en empate trivial.
        Setup esquivo = Solo(Mk(2, S(hp: 500, atk: 10, def: 0, spd: 10, eva: 10000)));

        BattleResult r = BattleSimulator.Simulate(atacante, esquivo, seed: 9, SetRegistry.Empty, Config);

        // Ningún ataque debe haber impactado a la criatura 2; todos sus eventos son evasiones.
        Assert.DoesNotContain(r.Log, e => e.Type == BattleEventType.Attack && e.TargetId == 2);
        Assert.Contains(r.Log, e => e.Type == BattleEventType.Evade && e.TargetId == 2);
    }

    [Fact]
    public void La_linea_frontal_recibe_el_dano_antes_que_la_retaguardia()
    {
        Setup atacante = Solo(Mk(1, S(hp: 1000, atk: 80, def: 0, spd: 50)));
        // Defensor: tanque frontal (id 10) + retaguardia (id 11).
        var tank = Mk(10, S(hp: 600, atk: 5, def: 20, spd: 5));
        var dps = Mk(11, S(hp: 100, atk: 5, def: 0, spd: 5));
        var defensor = new Setup(new[] { tank }, new[] { dps });

        BattleResult r = BattleSimulator.Simulate(atacante, defensor, seed: 3, SetRegistry.Empty, Config);

        BattleEvent firstAttackByA = r.Log.First(e => e.Type == BattleEventType.Attack && e.ActorId == 1);
        Assert.Equal(10, firstAttackByA.TargetId); // golpea al frontal primero

        // No se ataca a la retaguardia (11) hasta que el frontal (10) ha muerto.
        var log = r.Log.ToList();
        int firstHitOnBack = log.FindIndex(e => e.Type == BattleEventType.Attack && e.TargetId == 11);
        if (firstHitOnBack >= 0)
        {
            int tankDeath = log.FindIndex(e => e.Type == BattleEventType.Death && e.TargetId == 10);
            Assert.True(tankDeath >= 0 && tankDeath < firstHitOnBack);
        }
    }

    [Fact]
    public void El_set_otorga_keyword_y_aplica_veneno()
    {
        // Set "Abisal": con 2 piezas aplica veneno al golpear.
        var abisal = new SetDefinition("Abisal", new[]
        {
            SetThreshold.Of(2, Stats.Zero, CombatKeyword.ApplyPoisonOnHit),
        });
        var sets = new SetRegistry(new[] { abisal });

        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(100, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config),
            [AnatomySlot.Fangs] = PartFactory.Create(101, "Abisal", AnatomySlot.Fangs, Rarity.Fresh, Config),
        };

        Setup envenenador = Solo(Mk(1, S(hp: 1000, atk: 60, def: 0, spd: 50), equipped));
        Setup victima = Solo(Mk(2, S(hp: 1000, atk: 5, def: 0, spd: 10)));

        BattleResult r = BattleSimulator.Simulate(envenenador, victima, seed: 1, sets, Config);

        Assert.Contains(r.Log, e => e.Type == BattleEventType.PoisonTick && e.TargetId == 2 && e.Value > 0);
    }

    [Fact]
    public void Stats_resueltas_incluyen_partes_y_bonus_de_set()
    {
        var abisal = new SetDefinition("Abisal", new[]
        {
            SetThreshold.Of(2, new Stats(0, 0, 0, 100, 0, 0, 0, 0)),
        });
        var sets = new SetRegistry(new[] { abisal });

        var equipped = new Dictionary<AnatomySlot, Part>
        {
            [AnatomySlot.Claws] = PartFactory.Create(1, "Abisal", AnatomySlot.Claws, Rarity.Fresh, Config), // Attack 50
            [AnatomySlot.Fangs] = PartFactory.Create(2, "Abisal", AnatomySlot.Fangs, Rarity.Fresh, Config), // Attack 50
        };
        var creature = Mk(1, S(hp: 100, atk: 0, def: 0, spd: 10), equipped);

        ResolvedCreature resolved = StatsResolver.Resolve(creature, sets);

        Assert.Equal(100, resolved.Stats.Attack);  // 0 innato + 50 + 50
        Assert.Equal(110, resolved.Stats.Speed);   // 10 innato + 100 del set
        Assert.Empty(resolved.Keywords);           // este set solo da stats, ninguna keyword
    }

    [Fact]
    public void PowerRating_es_mayor_para_un_setup_mas_fuerte()
    {
        Setup fuerte = Solo(Mk(1, S(hp: 500, atk: 120, def: 30, spd: 40)));
        Setup debil = Solo(Mk(2, S(hp: 200, atk: 20, def: 5, spd: 20)));

        int pFuerte = PowerRating.Of(fuerte, SetRegistry.Empty);
        int pDebil = PowerRating.Of(debil, SetRegistry.Empty);

        Assert.True(pFuerte > pDebil, $"fuerte={pFuerte}, debil={pDebil}");
    }
}
