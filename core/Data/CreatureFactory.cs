using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Rng;

namespace TaskbarTamer.Core.Data;

/// <summary>
/// Genera criaturas nuevas con stats base variados de forma determinista (mismo
/// <c>seed</c> → misma criatura). Sin partes equipadas: el jugador las equipa después.
/// </summary>
public static class CreatureFactory
{
    private static readonly string[] Names =
    {
        "Mordak", "Gnasher", "Vesp", "Thorn", "Krul",
        "Sable", "Vortx", "Mire", "Rax", "Nyx", "Brood", "Skarn",
    };

    public static Creature Roll(long id, ulong seed)
    {
        var rng = new DeterministicRng(seed);

        string name = Names[rng.NextInt(Names.Length)];
        int hp = 30 + rng.NextInt(31);     // 30..60
        int atk = 3 + rng.NextInt(5);      // 3..7
        int def = 1 + rng.NextInt(4);      // 1..4
        int spd = 2 + rng.NextInt(5);      // 2..6

        return new Creature(id, name, new Stats(hp, atk, def, spd, 0, 0, 0, 0));
    }
}
