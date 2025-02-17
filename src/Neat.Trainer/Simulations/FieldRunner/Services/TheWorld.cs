using System.Drawing;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
using Neat.Trainer.Simulations.FieldRunner.Enums;
using Neat.Trainer.Simulations.FieldRunner.Models;
namespace Neat.Trainer.Simulations.FieldRunner.Services;

public class TheWorld
{
    private readonly WorldSettings _settings;
    private readonly PhenotypeRunner _pika;

    public TheWorld(IReadOnlyCollection<PhenotypeRunner> phenotypes, WorldSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _pika = phenotypes.Single() ?? throw new ArgumentNullException(nameof(phenotypes));
        World = WorldBuilder.CreateNewWorld(_settings, [_pika]);
    }

    public WorldData World { get; }

    public IReadOnlyCollection<SimulationResult> Simulate(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && EvaluateIteration())
        {
            // evaluate a new beautiful life cycle
        }

        return [new SimulationResult(_pika.Phenotype.Genome, MeasureFitness())];
    }

    private bool EvaluateIteration()
    {
        var lastIteration = World.Timeline.Peek();

        // copy field to a new iteration
        var newIteration = new WorldField
        {
            Cells = new WorldCell?[lastIteration.Cells.Length],
        };

        // copy items
        for (var i = 0; i < lastIteration.Cells.Length; i++)
        {
            if (lastIteration.Cells[i]?.Item?.Type is WorldItemType.Wall or WorldItemType.Food or WorldItemType.Poison)
                newIteration.Cells[i] = lastIteration.Cells[i];
        }

        // move characters
        var pikas = lastIteration.Cells
            .Select((x, i) => new
            {
                Index = i,
                Cell = x,
            })
            .Where(x => x.Cell?.Item?.Type == WorldItemType.Pika)
            .Shuffle()
            .ToList();

        var haveAlivePikas = false;
        foreach (var pikaCell in pikas)
            haveAlivePikas = haveAlivePikas || EvaluatePika(pikaCell.Index, (PikaWorldItem) pikaCell.Cell!.Item!, newIteration);

        World.Timeline.Push(newIteration);
        return haveAlivePikas;
    }

    private bool EvaluatePika(int cellIndex, PikaWorldItem oldPika, WorldField iteration)
    {
        var pikaPosition = PositionTool.IndexToPosition(cellIndex, World.Size);
        var moveDirection = MakeMoveDecision(oldPika, pikaPosition, iteration);

        var (targetPositions, targetDirection) = PositionTool.CalculateMove(pikaPosition, oldPika.Direction, moveDirection, World.Size);

        var targetIndex = PositionTool.PositionToIndex(targetPositions[0], World.Size);
        var targetItem = iteration.Cells[targetIndex]?.Item?.Type;

        var newPika = oldPika.Duplicate() with
        {
            Energy = oldPika.Energy - _settings.MoveCost,
            Direction = targetDirection,
        };

        if (targetItem == WorldItemType.Food)
        {
            // pika eats
            newPika = newPika with { Energy = newPika.Energy + _settings.FoodEnergy };
            iteration.Cells[targetIndex] = null;
        }

        if (targetItem == WorldItemType.Poison)
        {
            // pika penalized
            newPika = newPika with { Energy = newPika.Energy - _settings.PoisonPenaltyEnergy };
            iteration.Cells[targetIndex] = null;
        }

        if (targetItem == WorldItemType.Wall)
        {
            // pika penalized
            newPika = newPika with { Energy = newPika.Energy - _settings.WallPenaltyEnergy };
        }

        if (newPika.Energy <= 0)
        {
            // pika died
            iteration.Cells[cellIndex] = null;
            return false;
        }

        if (iteration.Cells[targetIndex] == null)
        {
            // pika moved
            iteration.Cells[targetIndex] = new WorldCell
            {
                Item = newPika,
            };
        }
        else
        {
            // pika stays
            iteration.Cells[cellIndex] = new WorldCell
            {
                Item = newPika,
            };
        }

        return true;
    }

    private Move? MakeMoveDecision(PikaWorldItem pika, Point position, WorldField iteration)
    {
        var inputs = new[]
        {
            LookAtDirection(pika, position, Move.Left, iteration, World, WorldItemType.Food)?.Distance ?? 0,
            LookAtDirection(pika, position, Move.Forward, iteration, World, WorldItemType.Food)?.Distance ?? 0,
            LookAtDirection(pika, position, Move.Right, iteration, World, WorldItemType.Food)?.Distance ?? 0,

            // LookAtDirection(pika, position, Move.Left, iteration, World, WorldItemType.Poison)?.Distance ?? 0,
            // LookAtDirection(pika, position, Move.Forward, iteration, World, WorldItemType.Poison)?.Distance ?? 0,
            // LookAtDirection(pika, position, Move.Right, iteration, World, WorldItemType.Poison)?.Distance ?? 0,

            LookAtDirection(pika, position, Move.Left, iteration, World, WorldItemType.Wall)?.Distance ?? 0,
            LookAtDirection(pika, position, Move.Forward, iteration, World, WorldItemType.Wall)?.Distance ?? 0,
            LookAtDirection(pika, position, Move.Right, iteration, World, WorldItemType.Wall)?.Distance ?? 0,

            // (float) ((Random.Shared.NextDouble() * 2f) - 1),
            // (float) Math.Sin(DateTime.Now.Millisecond),
        };

        var output = pika
            .PhenotypeRunner
            .Run(inputs);

        var decision = output
            .Select(x => new
            {
                Move = Enum.TryParse<Move>(x.Key.Data, out var move) ? move : (Move?) null,
                x.Value,
            })
            .Where(x => x.Move.HasValue)
            .MaxBy(x => x.Value);

        return decision?.Move;
    }

    private float MeasureFitness()
    {
        var daysSurvived = World
            .Timeline
            .Reverse()
            .TakeWhile(day => day.Cells.Any(cell => cell?.Item is PikaWorldItem))
            .Count();

        return daysSurvived;
    }

    private static (WorldItemType Type, float Distance)? LookAtDirection(PikaWorldItem pika, Point position, Move move, WorldField field, WorldData world, WorldItemType? search = null)
    {
        var calc = PositionTool.CalculateMove(position, pika.Direction, move, world.Size);
        for (var i = 0; i < calc.Positions.Length; i++)
        {
            var index = PositionTool.PositionToIndex(calc.Positions[i], world.Size);
            var item = field.Cells[index]?.Item;
            if (item == null || (search != null && item.Type != search)) continue;

            var maxDistance = calc.Direction is Direction.Left or Direction.Right
                ? world.Size.Width
                : world.Size.Height;

            var distance = 1f - (i / (float) maxDistance);
            return (item.Type, distance);
        }

        return null;
    }
}
