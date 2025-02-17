using Neat.Core.Genomes;
namespace Neat.Core.Evolution;

public record EvolutionArgs
{
    public required Genotype Parent1 { get; init; }
    public required Genotype Parent2 { get; init; }
    public required bool PreferParent1 { get; init; }
}
