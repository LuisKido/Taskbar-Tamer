namespace TaskbarTamer.Core.Model;

/// <summary>
/// Bloque de estadísticas. Todos los valores son <b>enteros</b> por determinismo
/// (ver docs/02 §3). Los porcentajes se expresan en <i>basis points</i>: base 10000,
/// es decir 1500 = 15%.
/// </summary>
public readonly record struct Stats(
    int MaxHp,
    int Attack,
    int Defense,
    int Speed,
    int CritChance,   // basis points (10000 = 100%)
    int CritDamage,   // basis points
    int Evasion,      // basis points
    int StatusPower)  // basis points
{
    public static readonly Stats Zero = default;

    public static Stats operator +(Stats a, Stats b) => new(
        a.MaxHp + b.MaxHp,
        a.Attack + b.Attack,
        a.Defense + b.Defense,
        a.Speed + b.Speed,
        a.CritChance + b.CritChance,
        a.CritDamage + b.CritDamage,
        a.Evasion + b.Evasion,
        a.StatusPower + b.StatusPower);

    /// <summary>
    /// Escala cada stat por un factor en basis points (10000 = x1.0). Usa aritmética
    /// de 64 bits internamente para evitar overflow, pero el resultado es entero.
    /// </summary>
    public Stats ScaleBp(int bp) => new(
        Scale(MaxHp, bp),
        Scale(Attack, bp),
        Scale(Defense, bp),
        Scale(Speed, bp),
        Scale(CritChance, bp),
        Scale(CritDamage, bp),
        Scale(Evasion, bp),
        Scale(StatusPower, bp));

    private static int Scale(int value, int bp) => (int)((long)value * bp / 10000);
}
