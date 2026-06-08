using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Simulation;

/// <summary>
/// Calcula un valor entero de "poder" de un setup a partir de sus stats efectivas.
/// Es el <c>TeamPower</c> que el sistema idle usa para decidir qué biomas puede
/// farmear el equipo (ver <c>FarmingSimulator</c>). Heurística, no exacta.
/// </summary>
public static class PowerRating
{
    public static int Of(Setup setup, SetRegistry sets) => Of(setup.All, sets);

    public static int Of(IEnumerable<Creature> creatures, SetRegistry sets)
    {
        long total = 0;
        foreach (Creature creature in creatures)
        {
            Stats s = StatsResolver.Resolve(creature, sets).Stats;
            total += s.Attack
                   + s.Defense
                   + s.MaxHp / 10
                   + s.Speed
                   + (s.CritChance + s.Evasion + s.StatusPower) / 100;
        }
        return (int)total;
    }
}
