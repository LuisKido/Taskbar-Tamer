using TaskbarTamer.Core.Model;

namespace TaskbarTamer.Core.Simulation;

/// <summary>
/// Formación de combate de un jugador: línea frontal (absorbe daño) y retaguardia
/// (daño / estados). El posicionamiento determina las reglas de targeting.
/// </summary>
public sealed class Setup
{
    public IReadOnlyList<Creature> FrontLine { get; }
    public IReadOnlyList<Creature> BackLine { get; }

    public Setup(IReadOnlyList<Creature> frontLine, IReadOnlyList<Creature> backLine)
    {
        if (frontLine.Count == 0 && backLine.Count == 0)
            throw new ArgumentException("Un setup necesita al menos una criatura.");
        FrontLine = frontLine;
        BackLine = backLine;
    }

    public IEnumerable<Creature> All
    {
        get
        {
            foreach (Creature c in FrontLine) yield return c;
            foreach (Creature c in BackLine) yield return c;
        }
    }
}
