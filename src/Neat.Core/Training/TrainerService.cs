using System.Collections.Concurrent;
using System.Diagnostics;
using Neat.Core.Genomes;
namespace Neat.Core.Training;

// TODO move it to Neat.Core
public class TrainerService
{
    private readonly SimulationProvider _simulationProvider;
    private readonly TrainingSettings _trainingSettings;

    public TrainerService(SimulationProvider simulationProvider, TrainingSettings settings)
    {
        _simulationProvider = simulationProvider ?? throw new ArgumentNullException(nameof(simulationProvider));
        _trainingSettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlyCollection<Genotype> Run(IReadOnlyCollection<Genotype> genomes, CancellationToken cancellationToken)
    {
        Debug.Assert(_trainingSettings.SimulationsAtOnce >= genomes.Count, $"Simulations at once ({_trainingSettings.SimulationsAtOnce}) must be greater or equal to genomes count ({genomes.Count})");

        // run X simulations
        var genomesLoop = new ConcurrentLoop<Genotype>(genomes);
        var simulations = _trainingSettings
            .SimulationsAtOnce
            .Times(() => _simulationProvider.Provide(genomesLoop))
            .ToList();

        var results = new ConcurrentBag<SimulationResult>();
        Parallel.ForEach(simulations, new ParallelOptions { MaxDegreeOfParallelism = -1 }, simulation =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            var result = simulation.Run(cancellationToken);
            results.AddRange(result);
        });

        // TODO save statistic

        return results
            .Select(x => x.Genome with
            {
                HistoricalFitness = x.Fitness,
                Age = x.Genome.Age + 1,
            })
            .ToList();
    }
}
