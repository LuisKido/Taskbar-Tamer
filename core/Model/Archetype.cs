namespace TaskbarTamer.Core.Model;

/// <summary>
/// Arquetipo (especie) de una criatura: define su <b>habilidad de arena</b> intrínseca
/// y su rol. Existe exactamente una criatura por arquetipo; se desbloquean (no se
/// reclutan al azar). El equipo equipado da stats y aspecto, pero NO cambia el arquetipo.
/// </summary>
public enum Archetype
{
    /// <summary>Bruiser ofensivo — habilidad: Tajo (golpe en área). Valor por defecto (saves antiguos).</summary>
    Bruiser = 0,

    /// <summary>Tanque — habilidad: Provocar (atrae enemigos).</summary>
    Guardian = 1,

    /// <summary>Cargador veloz — habilidad: Embestida (dash + AoE).</summary>
    Charger = 2,

    /// <summary>Saltador — habilidad: Salto (brinco + golpe).</summary>
    Leaper = 3,

    /// <summary>Venenoso — habilidad: Estallido tóxico (AoE veneno).</summary>
    Venomous = 4,
}
