using System.Drawing;
namespace Neat.Trainer.Simulations.FieldRunner.Models;

public record WorldData
{
    public Guid Id { get; } = Guid.NewGuid();
    public required Size Size { get; init; }
    public Stack<WorldField> Timeline { get; } = new ();
}

public record WorldField
{
    public required WorldCell?[] Cells { get; init; }
}

public record WorldCell
{
    public WorldItem? Item { get; init; }
}
