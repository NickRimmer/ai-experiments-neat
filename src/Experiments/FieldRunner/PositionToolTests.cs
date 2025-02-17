using System.Drawing;
using World.FieldRunner.Game.Enums;
using World.FieldRunner.Game.Services;
namespace Experiments.FieldRunner;

public class PositionToolTests
{
    [TestCase(Direction.Up, Move.Left, Direction.Left)]
    [TestCase(Direction.Up, Move.Right, Direction.Right)]
    [TestCase(Direction.Up, Move.Forward, Direction.Up)]
    [TestCase(Direction.Down, Move.Left, Direction.Right)]
    [TestCase(Direction.Down, Move.Right, Direction.Left)]
    [TestCase(Direction.Down, Move.Forward, Direction.Down)]
    public void NewDirection(Direction current, Move move, Direction expected) =>
        PositionTool.CalculateMove(Point.Empty, current, move, new Size(10, 10)).Direction.Should().Be(expected);

    [TestCase(Direction.Up, Move.Left, 4, 5)]
    [TestCase(Direction.Up, Move.Right, 6, 5)]
    [TestCase(Direction.Up, Move.Forward, 5, 4)]
    [TestCase(Direction.Down, Move.Left, 6, 5)]
    [TestCase(Direction.Down, Move.Right, 4, 5)]
    [TestCase(Direction.Down, Move.Forward, 5, 6)]
    public void NewPosition(Direction current, Move move, int newX, int newY) =>
        PositionTool.CalculateMove(new Point(5, 5), current, move, new Size(10, 10)).Positions[0].Should().Be(new Point(newX, newY));
}
