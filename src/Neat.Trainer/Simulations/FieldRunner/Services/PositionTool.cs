using System.Drawing;
using Neat.Trainer.Simulations.FieldRunner.Enums;
namespace Neat.Trainer.Simulations.FieldRunner.Services;

public static class PositionTool
{
    private static readonly Random Rnd = new ();

    public static Point IndexToPosition(int index, Size worldSize)
    {
        var x = index % worldSize.Width;
        var y = index / worldSize.Width;
        return new Point(x, y);
    }

    public static int PositionToIndex(Point targetPosition, Size worldSize)
    {
        if (targetPosition.X < 0 || targetPosition.X >= worldSize.Width || targetPosition.Y < 0 || targetPosition.Y >= worldSize.Height)
            throw new ArgumentOutOfRangeException(nameof(targetPosition), targetPosition, "Position is out of world bounds");

        return (targetPosition.Y * worldSize.Width) + targetPosition.X;
    }

    public static Direction GetRandomDirection() => (Direction) Rnd.Next(0, 4);

    public static (Point[] Positions, Direction Direction) CalculateMove(Point currentPosition, Direction currentDirection, Move? move, Size worldSize)
    {
        var newDirection = move switch
        {
            null => currentDirection,
            Move.Forward => currentDirection,
            Move.Left => currentDirection switch
            {
                Direction.Up => Direction.Left,
                Direction.Left => Direction.Down,
                Direction.Down => Direction.Right,
                Direction.Right => Direction.Up,
                _ => throw new ArgumentOutOfRangeException(nameof(currentDirection), currentDirection, null),
            },
            Move.Right => currentDirection switch
            {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Left,
                Direction.Left => Direction.Up,
                _ => throw new ArgumentOutOfRangeException(nameof(currentDirection), currentDirection, null),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(move), move, null),
        };

        var newPositions = newDirection switch
        {
            _ when move == null => [currentPosition],
            Direction.Up => GetRange(currentPosition.Y - 1, 0).Select(y => new Point(currentPosition.X, y)),
            Direction.Right => GetRange(currentPosition.X + 1, worldSize.Width - 1).Select(x => new Point(x, currentPosition.Y)),
            Direction.Down => GetRange(currentPosition.Y + 1, worldSize.Height - 1).Select(y => new Point(currentPosition.X, y)),
            Direction.Left => GetRange(currentPosition.X - 1, 0).Select(x => new Point(x, currentPosition.Y)),
            _ => throw new ArgumentOutOfRangeException(nameof(currentDirection), currentDirection, null),
        };

        return (newPositions.ToArray(), newDirection);
    }

    private static IEnumerable<int> GetRange(int start, int stop)
    {
        if (start < stop)
        {
            for (var i = start; i <= stop; i++)
                yield return i;
        }
        else
        {
            for (var i = start; i >= stop; i--)
                yield return i;
        }
    }
}
