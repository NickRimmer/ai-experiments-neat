using Neat.Core.Genomes;
namespace World.FieldRunner.Game.Models;

public record SimulationResult
{
    public required WorldModel World { get; init; }
    public required IReadOnlyCollection<SimulationPika> Pikas { get; init; }
}

public record SimulationPika
{
    public required Genotype Genome { get; init; }
    public required double Fitness { get; init; }
}
