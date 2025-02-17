using System.Drawing;
namespace Neat.Trainer.Simulations.FieldRunner.Models;

public record WorldSettings
{
    public required Size WorldSize { get; init; }
    public required int InitialFoodCount { get; init; }
    public int PikaStartEnergy { get; init; } = 50;
    public int FoodEnergy { get; init; } = 15;
    public int PoisonPenaltyEnergy { get; set; } = 25;
    public int WallPenaltyEnergy { get; set; } = 5;

    public int MoveCost { get; init; } = 1;
    public int ObstaclesCount { get; set; }
    public int PoisonsCount { get; set; }
}
