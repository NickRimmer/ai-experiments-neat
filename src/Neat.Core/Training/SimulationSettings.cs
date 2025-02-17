namespace Neat.Core.Training;

public record SimulationSettings
{
    public required string Name { get; init; }
    public int Population { get; init; } = 50;
}
