using Neat.Core.Genomes;
namespace Neat.Core.Species;

public record Specie
{
    private const float RockStarEffect = .2f;

    public required IReadOnlyCollection<Genotype> Genomes { get; init; }
    public float AverageFitness =>
        (RockStarEffect * Genomes.Max(x => x.HistoricalFitness)) + ((1f - RockStarEffect) * Genomes.Average(x => x.HistoricalFitness));

    // public float AverageFitness => Genomes.Average(x => x.HistoricalFitness);
}
