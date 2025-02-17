using Neat.Core.Genomes;
namespace Neat.Core.Training;

public interface ISimulation
{
    void Initialize(ConcurrentLoop<Genotype> genomes);
    IReadOnlyCollection<SimulationResult> Run(CancellationToken cancellationToken);
    IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context);
}
