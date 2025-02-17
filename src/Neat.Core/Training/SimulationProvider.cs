using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Neat.Core.Genomes;

namespace Neat.Core.Training;

public class SimulationProvider
{
    private readonly IServiceProvider _services;
    private readonly GenomesContext _context;
    private readonly string _simulationName;
    private readonly int _populationCount;

    public SimulationProvider(SimulationSettings settings, IServiceProvider services, GenomesContext context)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _simulationName = settings.Name;
        _populationCount = settings.Population;
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation()
    {
        var simulation = BuildSimulation();
        var result = simulation.BuildInitialPopulation(_populationCount, _context);
        Debug.Assert(result.Distinct().Count() == result.Count);

        return result;
    }

    public ISimulation Provide(ConcurrentLoop<Genotype> genomes)
    {
        var simulation = BuildSimulation();
        simulation.Initialize(genomes);
        return simulation;
    }

    private ISimulation BuildSimulation()
    {
        var simulation = _services.GetKeyedService<ISimulation>(_simulationName);
        if (simulation == null)
        {
            Log.Fatal("Simulation not found: {Simulation}", _simulationName);
            throw new InvalidOperationException("Simulation not found");
        }

        return simulation;
    }
}
