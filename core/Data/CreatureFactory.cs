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
        int hp = 220 + rng.NextInt(161);   // 220..380
        int atk = 14 + rng.NextInt(17);    // 14..30
        int def = 6 + rng.NextInt(13);     // 6..18
        int spd = 8 + rng.NextInt(17);     // 8..24

        return new Creature(id, name, new Stats(hp, atk, def, spd, 0, 0, 0, 0));
    }
}
