using System.Drawing;
using Neat.Core.Common;
using Neat.Core.Phenotypes;
using World.FieldRunner.Game.Enums;
using World.FieldRunner.Game.Models;
namespace World.FieldRunner.Game.Services;

public class Simulation
{
    private readonly SimulationSettings _settings;
    private readonly IReadOnlyCollection<Phenotype> _phenotypes;
    private static readonly Random Rnd = new ();
    private readonly WorldModel _world;

    public Simulation(SimulationSettings settings, IReadOnlyCollection<Phenotype> phenotypes)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _phenotypes = phenotypes ?? throw new ArgumentNullException(nameof(phenotypes));
        _world = CreateNewWorld(settings, phenotypes);
    }

    public Task<SimulationResult?> StartAsync(CancellationToken cancellationToken) =>
        Task.Run(() => Start(cancellationToken), cancellationToken);

    public SimulationResult? Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && EvaluateIteration())
        {
            // evaluate a new beautiful life cycle
        }

        return cancellationToken.IsCancellationRequested ? null : new SimulationResult
        {
            World = _world,
            Pikas = MeasureFitness(_world, _phenotypes),
        };
    }

    private bool EvaluateIteration()
    {
        var lastIteration = _world.Timeline.Peek();

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
            .OrderBy(_ => Rnd.NextDouble())
            .ToList();

        var haveAlivePikas = false;
        foreach (var pikaCell in pikas)
            haveAlivePikas = haveAlivePikas || EvaluatePika(pikaCell.Index, (PikaWorldItem) pikaCell.Cell!.Item!, newIteration);

        _world.Timeline.Push(newIteration);
        return haveAlivePikas;
    }

    private bool EvaluatePika(int cellIndex, PikaWorldItem oldPika, WorldField iteration)
    {
        var pikaPosition = PositionTool.IndexToPosition(cellIndex, _world.Size);
        var moveDirection = MakeMoveDecision(oldPika, pikaPosition, iteration);

        var (targetPositions, targetDirection) = PositionTool.CalculateMove(pikaPosition, oldPika.Direction, moveDirection, _world.Size);

        var targetIndex = PositionTool.PositionToIndex(targetPositions[0], _world.Size);
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
        // var inputs = new[]
        // {
        //     LookAtDirection(pika, position, Move.Left, iteration, _world, WorldItemType.Food)?.Distance,
        //     LookAtDirection(pika, position, Move.Forward, iteration, _world, WorldItemType.Food)?.Distance,
        //     LookAtDirection(pika, position, Move.Right, iteration, _world, WorldItemType.Food)?.Distance,
        //
        //     LookAtDirection(pika, position, Move.Left, iteration, _world, WorldItemType.Poison)?.Distance,
        //     LookAtDirection(pika, position, Move.Forward, iteration, _world, WorldItemType.Poison)?.Distance,
        //     LookAtDirection(pika, position, Move.Right, iteration, _world, WorldItemType.Poison)?.Distance,
        //
        //     LookAtDirection(pika, position, Move.Left, iteration, _world, WorldItemType.Wall)?.Distance,
        //     LookAtDirection(pika, position, Move.Forward, iteration, _world, WorldItemType.Wall)?.Distance,
        //     LookAtDirection(pika, position, Move.Right, iteration, _world, WorldItemType.Wall)?.Distance,
        //
        //     (float) ((Rnd.NextDouble() * 2f) - 1),
        //     (float) Math.Sin(DateTime.Now.Millisecond),
        // };
        //
        // var output = pika
        //     .PhenotypeRunner
        //     .Run(inputs);
        //
        // var decision = output
        //     .Select(x => new
        //     {
        //         Move = Enum.TryParse<Move>(x.Key.Data, out var move) ? move : (Move?) null,
        //         x.Value,
        //     })
        //     .Where(x => x.Move.HasValue)
        //     .MaxBy(x => x.Value);
        //
        // return decision?.Move;

        throw new NotImplementedException();
    }

    private static (WorldItemType Type, float Distance)? LookAtDirection(PikaWorldItem pika, Point position, Move move, WorldField field, WorldModel world, WorldItemType? search = null)
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

            var distance = i / (float) maxDistance;
            return (item.Type, distance);
        }

        return null;
    }

    private static IReadOnlyCollection<SimulationPika> MeasureFitness(WorldModel world, IReadOnlyCollection<Phenotype> phenotypes) => phenotypes
        .ToList(phenotype =>
        {
            var daysSurvived = world
                .Timeline
                .Reverse()
                .TakeWhile(day => day.Cells.Any(cell => cell?.Item is PikaWorldItem pika && pika.PhenotypeRunner.Phenotype == phenotype))
                .Count();

            return new SimulationPika
            {
                Fitness = daysSurvived,
                Genome = phenotype.Genome,
            };
        });

    private static WorldModel CreateNewWorld(SimulationSettings settings, IReadOnlyCollection<Phenotype> phenotypes)
    {
        var world = new WorldModel
        {
            Size = settings.WorldSize,
        };

        // create empty field
        var field = new WorldField
        {
            Cells = new WorldCell?[settings.WorldSize.Width * settings.WorldSize.Height],
        };

        // add border walls
        for (var i = 0; i < field.Cells.Length; i++)
        {
            var position = PositionTool.IndexToPosition(i, settings.WorldSize);

            if (position.X == 0 || position.X == settings.WorldSize.Width - 1 || position.Y == 0 || position.Y == settings.WorldSize.Height - 1)
            {
                field.Cells[i] = new WorldCell
                {
                    Item = new WorldItem(WorldItemType.Wall),
                };
            }
        }

        // prepare target cells in random order
        var emptyCellIds = new Stack<int>(
            field
                .Cells
                .Select((x, i) => new
                {
                    Index = i,
                    Cell = x,
                })
                .Where(x => x.Cell == null)
                .OrderBy(_ => Rnd.NextDouble())
                .Select(x => x.Index));

        // add obstacles
        if (emptyCellIds.Count <= 0) throw new InvalidOperationException("No empty cells");
        for (var i = 0; i < settings.ObstaclesCount; i++)
        {
            var index = emptyCellIds.Pop();
            field.Cells[index] = new WorldCell
            {
                Item = new WorldItem(WorldItemType.Wall),
            };
        }

        // add food
        if (emptyCellIds.Count <= 0) throw new InvalidOperationException("No empty cells");
        for (var i = 0; i < settings.InitialFoodCount; i++)
        {
            var index = emptyCellIds.Pop();
            field.Cells[index] = new WorldCell
            {
                Item = new WorldItem(WorldItemType.Food),
            };
        }

        // add poison
        if (emptyCellIds.Count <= 0) throw new InvalidOperationException("No empty cells");
        for (var i = 0; i < settings.PoisonsCount; i++)
        {
            var index = emptyCellIds.Pop();
            field.Cells[index] = new WorldCell
            {
                Item = new WorldItem(WorldItemType.Poison),
            };
        }

        // add character
        if (emptyCellIds.Count <= 0) throw new InvalidOperationException("No empty cells");
        foreach (var phenotype in phenotypes)
        {
            if (!emptyCellIds.TryPop(out var pikaCell)) throw new InvalidOperationException("Not enough space for pikas");
            field.Cells[pikaCell] = new WorldCell
            {
                Item = new PikaWorldItem
                {
                    Energy = settings.PikaStartEnergy,
                    Direction = PositionTool.GetRandomDirection(),
                    PhenotypeRunner = new PhenotypeRunner(phenotype),
                },
            };
        }

        // push field to timeline
        world.Timeline.Push(field);

        return world;
    }
}
