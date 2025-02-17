using System.Drawing;
namespace World.FieldRunner.Game.Models;

public record SimulationSettings
{
    public string GenePoolName { get; set; } = "default";
    public int GenePoolSize { get; set; } = 100;
    public int GeneSaveEvery { get; set; } = 5_000;

    public required Size WorldSize { get; init; }
    public required int InitialFoodCount { get; init; }
    public int PikaStartEnergy { get; init; } = 50;
    public int FoodEnergy { get; init; } = 10;
    public int PoisonPenaltyEnergy { get; set; } = 25;

    public int MoveCost { get; init; } = 1;
    public double FittestPercentage { get; set; } = .2; // 20%
    public int ObstaclesCount { get; set; }
    public int PoisonsCount { get; set; }
}
