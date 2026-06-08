namespace TaskbarTamer.Core.Model;

/// <summary>
/// Palabras clave de combate que una criatura puede obtener (normalmente vía
/// sinergias de set). El simulador las lee para activar comportamientos especiales.
/// </summary>
public enum CombatKeyword
{
    /// <summary>Cada golpe aplica stacks de veneno al objetivo.</summary>
    ApplyPoisonOnHit,
}
