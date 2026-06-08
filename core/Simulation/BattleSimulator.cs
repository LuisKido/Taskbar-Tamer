using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Rng;

namespace TaskbarTamer.Core.Simulation;

/// <summary>
/// Auto-battler determinista. <c>Simulate(a, b, seed, ...)</c> es una función pura:
/// el mismo par de setups + semilla produce siempre el mismo log y desenlace, en
/// cualquier máquina. Esto permite que la previsualización del cliente coincida con
/// el veredicto del servidor (ver docs/02 §3).
/// </summary>
public static class BattleSimulator
{
    private const int FrontLine = 0;
    private const int BackLine = 1;

    /// <summary>Estado mutable de una criatura durante la batalla.</summary>
    private sealed class Combatant
    {
        public required int Team;        // 0 = A, 1 = B
        public required int Line;        // 0 = frontal, 1 = retaguardia
        public required int Index;       // posición dentro de la línea
        public required long SourceId;   // Creature.Id, para el log
        public required Stats Stats;
        public required HashSet<CombatKeyword> Keywords;
        public required int CurrentHp;
        public int PoisonStacks;
        public int PoisonRounds;

        public bool Alive => CurrentHp > 0;
    }

    public static BattleResult Simulate(
        Setup teamA,
        Setup teamB,
        ulong seed,
        SetRegistry sets,
        GameConfig config)
    {
        var rng = new DeterministicRng(seed);
        var log = new List<BattleEvent>();

        List<Combatant> all = new();
        all.AddRange(BuildTeam(teamA, team: 0, sets));
        all.AddRange(BuildTeam(teamB, team: 1, sets));

        int round = 0;
        for (round = 1; round <= config.MaxRounds; round++)
        {
            TickPoison(all, round, log, config);
            if (IsTeamWiped(all, 0) || IsTeamWiped(all, 1))
                break;

            // Orden de acción de la ronda: por velocidad desc, con desempate total
            // (equipo, línea, índice) para ser determinista.
            var order = all.Where(c => c.Alive).ToList();
            order.Sort(CompareActionOrder);

            foreach (Combatant actor in order)
            {
                if (!actor.Alive)
                    continue;

                Combatant? target = SelectTarget(all, enemyTeam: 1 - actor.Team);
                if (target is null)
                    break; // equipo enemigo aniquilado

                ResolveAttack(actor, target, round, rng, log, config);
            }

            if (IsTeamWiped(all, 0) || IsTeamWiped(all, 1))
                break;
        }

        int roundsPlayed = Math.Min(round, config.MaxRounds);
        BattleOutcome outcome = DecideOutcome(all);
        return new BattleResult(outcome, roundsPlayed, log);
    }

    private static IEnumerable<Combatant> BuildTeam(Setup setup, int team, SetRegistry sets)
    {
        return Build(setup.FrontLine, FrontLine).Concat(Build(setup.BackLine, BackLine));

        IEnumerable<Combatant> Build(IReadOnlyList<Creature> line, int lineIndex)
        {
            for (int i = 0; i < line.Count; i++)
            {
                ResolvedCreature resolved = StatsResolver.Resolve(line[i], sets);
                yield return new Combatant
                {
                    Team = team,
                    Line = lineIndex,
                    Index = i,
                    SourceId = line[i].Id,
                    Stats = resolved.Stats,
                    Keywords = new HashSet<CombatKeyword>(resolved.Keywords),
                    CurrentHp = resolved.Stats.MaxHp,
                };
            }
        }
    }

    private static void TickPoison(List<Combatant> all, int round, List<BattleEvent> log, GameConfig config)
    {
        // Orden estable para que el log sea determinista.
        var poisoned = all.Where(c => c.Alive && c.PoisonRounds > 0).ToList();
        poisoned.Sort(CompareStablePosition);

        foreach (Combatant c in poisoned)
        {
            int damage = c.PoisonStacks * config.PoisonDamagePerStack;
            c.CurrentHp -= damage;
            c.PoisonRounds--;
            if (c.PoisonRounds == 0)
                c.PoisonStacks = 0;

            log.Add(new BattleEvent(round, BattleEventType.PoisonTick, c.SourceId, c.SourceId, damage, Crit: false));
            if (!c.Alive)
                log.Add(new BattleEvent(round, BattleEventType.Death, c.SourceId, c.SourceId, 0, Crit: false));
        }
    }

    private static void ResolveAttack(
        Combatant attacker, Combatant target, int round,
        DeterministicRng rng, List<BattleEvent> log, GameConfig config)
    {
        // Evasión (solo se tira si el objetivo tiene evasión, para no consumir RNG de más).
        if (target.Stats.Evasion > 0 && rng.NextInt(10000) < target.Stats.Evasion)
        {
            log.Add(new BattleEvent(round, BattleEventType.Evade, attacker.SourceId, target.SourceId, 0, Crit: false));
            return;
        }

        // Daño base con mitigación por defensa: atk * K / (K + def).
        long damage = (long)attacker.Stats.Attack * config.DefenseConstant
                      / (config.DefenseConstant + target.Stats.Defense);

        // Crítico.
        bool crit = attacker.Stats.CritChance > 0 && rng.NextInt(10000) < attacker.Stats.CritChance;
        if (crit)
        {
            int multBp = config.CritBaseBp + attacker.Stats.CritDamage;
            damage = damage * multBp / 10000;
        }

        if (damage < 1)
            damage = 1; // un golpe siempre hace al menos 1

        target.CurrentHp -= (int)damage;
        log.Add(new BattleEvent(round, BattleEventType.Attack, attacker.SourceId, target.SourceId, (int)damage, crit));

        // Veneno al golpear.
        if (attacker.Keywords.Contains(CombatKeyword.ApplyPoisonOnHit))
        {
            target.PoisonStacks += config.PoisonStacksPerHit;
            target.PoisonRounds = config.PoisonDuration;
        }

        if (!target.Alive)
            log.Add(new BattleEvent(round, BattleEventType.Death, attacker.SourceId, target.SourceId, 0, Crit: false));
    }

    /// <summary>
    /// Targeting: la línea frontal enemiga se ataca primero (criatura viva de menor
    /// índice); solo cuando cae toda la frontal se pasa a la retaguardia.
    /// </summary>
    private static Combatant? SelectTarget(List<Combatant> all, int enemyTeam)
    {
        Combatant? best = null;
        foreach (Combatant c in all)
        {
            if (c.Team != enemyTeam || !c.Alive)
                continue;
            if (best is null || ComparePositionPriority(c, best) < 0)
                best = c;
        }
        return best;
    }

    // Frontal antes que retaguardia; dentro de la línea, menor índice primero.
    private static int ComparePositionPriority(Combatant a, Combatant b)
    {
        int byLine = a.Line.CompareTo(b.Line);
        return byLine != 0 ? byLine : a.Index.CompareTo(b.Index);
    }

    private static int CompareActionOrder(Combatant a, Combatant b)
    {
        int bySpeed = b.Stats.Speed.CompareTo(a.Stats.Speed); // desc
        if (bySpeed != 0) return bySpeed;
        return CompareStablePosition(a, b);
    }

    private static int CompareStablePosition(Combatant a, Combatant b)
    {
        int byTeam = a.Team.CompareTo(b.Team);
        if (byTeam != 0) return byTeam;
        int byLine = a.Line.CompareTo(b.Line);
        if (byLine != 0) return byLine;
        return a.Index.CompareTo(b.Index);
    }

    private static bool IsTeamWiped(List<Combatant> all, int team)
    {
        foreach (Combatant c in all)
            if (c.Team == team && c.Alive)
                return false;
        return true;
    }

    private static BattleOutcome DecideOutcome(List<Combatant> all)
    {
        bool aAlive = !IsTeamWiped(all, 0);
        bool bAlive = !IsTeamWiped(all, 1);

        if (aAlive && !bAlive) return BattleOutcome.TeamA;
        if (bAlive && !aAlive) return BattleOutcome.TeamB;

        // Ambos vivos (timeout) o ambos muertos: desempate por HP restante total.
        long hpA = SumHp(all, 0);
        long hpB = SumHp(all, 1);
        if (hpA > hpB) return BattleOutcome.TeamA;
        if (hpB > hpA) return BattleOutcome.TeamB;
        return BattleOutcome.Draw;
    }

    private static long SumHp(List<Combatant> all, int team)
    {
        long sum = 0;
        foreach (Combatant c in all)
            if (c.Team == team && c.Alive)
                sum += c.CurrentHp;
        return sum;
    }
}
