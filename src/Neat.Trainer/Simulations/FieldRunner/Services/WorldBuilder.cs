using Neat.Core.Phenotypes;
using Neat.Trainer.Simulations.FieldRunner.Enums;
using Neat.Trainer.Simulations.FieldRunner.Models;
namespace Neat.Trainer.Simulations.FieldRunner.Services;

public static class WorldBuilder
{
    public static WorldData CreateNewWorld(WorldSettings settings, IReadOnlyCollection<PhenotypeRunner> pikas)
    {
        var world = new WorldData
        {
            Size = settings.WorldSize,
        };

        var field = CreateEmptyField(settings);

        // prepare target cells in random order
        var emptyCells = new Stack<int>(
            field
                .Cells
                .Select((x, i) => new
                {
                    Index = i,
                    Cell = x,
                })
                .Where(x => x.Cell == null)
                .OrderBy(_ => Random.Shared.NextDouble())
                .Select(x => x.Index));

        field.AddItems(emptyCells, settings.ObstaclesCount, () => new WorldItem(WorldItemType.Wall));
        field.AddItems(emptyCells, settings.InitialFoodCount, () => new WorldItem(WorldItemType.Food));
        // field.AddItems(emptyCells, settings.PoisonsCount, () => new WorldItem(WorldItemType.Poison));

        // add character
        if (emptyCells.Count <= 0) throw new InvalidOperationException("No empty cells");
        foreach (var pika in pikas)
        {
            if (!emptyCells.TryPop(out var pikaCell)) throw new InvalidOperationException("Not enough space for pikas");
            field.Cells[pikaCell] = new WorldCell
            {
                Item = new PikaWorldItem
                {
                    Energy = settings.PikaStartEnergy,
                    Direction = PositionTool.GetRandomDirection(),
                    PhenotypeRunner = pika,
                },
            };
        }

        // push field to timeline
        world.Timeline.Push(field);
        return world;
    }

    private static WorldField CreateEmptyField(WorldSettings settings)
    {
        var field = new WorldField()
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

        return field;
    }

    private static void AddItems(
        this WorldField field,
        Stack<int> emptyCells,
        int count,
        Func<WorldItem> itemFactory)
    {
        if (emptyCells.Count <= count) throw new InvalidOperationException("Not enough empty cells");
        for (var i = 0; i < count; i++)
        {
            var index = emptyCells.Pop();
            field.Cells[index] = new WorldCell
            {
                Item = itemFactory.Invoke(),
            };
        }
    }
}
