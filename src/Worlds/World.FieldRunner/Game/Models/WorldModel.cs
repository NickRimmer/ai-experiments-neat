using System.Drawing;
namespace World.FieldRunner.Game.Models;

public record WorldModel
{
    public required Size Size { get; init; }
    public Stack<WorldField> Timeline { get; } = new ();
    public Guid Id { get; } = Guid.NewGuid();
}

public record WorldField
{
    public required WorldCell?[] Cells { get; init; }
}

public record WorldCell
{
    public WorldItem? Item { get; init; }
}
