using Neat.Core.Evolution;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using World.FieldRunner.Game.Models;
namespace World.FieldRunner.Game.Services;

public class TrainingService
{
    private static readonly Random Rnd = new ();

    private readonly List<(double, double)> _history = new ();
    private readonly EvolutionSettings _evolutionSettings = new ();
    private int _lastSavedCount;
    private CancellationTokenSource? _cts;

    private TrainingService(SimulationSettings? settings)
    {
        Settings = settings;
    }

    public SimulationSettings? Settings { get; }
    public static TrainingService Instance { get; private set; } = new (null);
    public SimulationResult? LastSimulation { get; private set; }
    public SimulationResult? BestSimulation { get; private set; }
    public IReadOnlyCollection<(double Best, double Worst)> History => _history;
    public IReadOnlyCollection<Genotype> Genomes = [];

    public bool IsRunning => _cts != null;
    public int SimulationsCount => _history.Count;

    public async Task StartAsync()
    {
        if (Settings == null) return;
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        Genomes = GenomesPool.Read(Settings.GenePoolName);
        if (Genomes.Count == 0) Genomes = GenomesPool.Create(Settings.GenePoolName, Settings.GenePoolSize);

        while (!_cts.IsCancellationRequested)
        {
            var results = await EvaluateIterationAsync(Settings, Genomes, _cts.Token);
            Genomes = EvaluateGenomes(results, Genomes, Settings);
        }

        GenomesPool.Save(Settings.GenePoolName, Genomes);
        _cts = null;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    public async Task ReplayAsync()
    {
        if (Settings == null) return;

        if (Genomes is not { Count: > 0 }) Genomes = GenomesPool.Read(Settings.GenePoolName);
        if (Genomes == null) return;

        LastSimulation = (await EvaluateIterationAsync(Settings, [Genomes.First()], CancellationToken.None))
            .OrderByDescending(x => x.Pikas.Max(y => y.Fitness))
            .FirstOrDefault();
    }

    public static TrainingService Setup(SimulationSettings settings)
    {
        if (Instance?._cts != null) throw new InvalidOperationException("Cannot setup training service while it is running.");

        Instance = new TrainingService(settings);
        return Instance;
    }

    private async Task<IReadOnlyCollection<SimulationResult>> EvaluateIterationAsync(SimulationSettings settings, IReadOnlyCollection<Genotype> genomes, CancellationToken cancellationToken)
    {
        var simulations = genomes

            // create phenotypes
            .Select(genome => PhenotypeBuilder.TryBuild(
                genome,
                out var result) ? result : throw new InvalidOperationException("Cannot build phenotype"))

            // create simulations
            .Select(phenotype => new Simulation(settings, [phenotype]))
            .Select(simulation => simulation.StartAsync(cancellationToken));

        // simulate them all
        try
        {
            var results = (await Task.WhenAll(simulations))
                .OfType<SimulationResult>()
                .ToList();

            if (results.Count == 0) throw new InvalidOperationException("No simulations were completed.");
            _history.AddRange(results.Select(x => (
                x.Pikas.Max(y => y.Fitness),
                x.Pikas.Min(y => y.Fitness)
            )));

            LastSimulation = results.OrderByDescending(x => x.Pikas.Max(y => y.Fitness)).First();
            BestSimulation = results.Append(BestSimulation).OfType<SimulationResult>().OrderByDescending(x => x.Pikas.Max(y => y.Fitness)).First();
            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // TODO for debug
            throw;
        }
    }

    private IReadOnlyCollection<Genotype> EvaluateGenomes(IReadOnlyCollection<SimulationResult> results, IReadOnlyCollection<Genotype> genomes, SimulationSettings settings)
    {
        // var bestEverPika = BestSimulation?
        //     .Pikas
        //     .OrderByDescending(x => x.Fitness)
        //     .FirstOrDefault();
        //
        // // select best genomes
        // var bestGenomes = results
        //     .SelectMany(x => x.Pikas)
        //     .OrderByDescending(x => x.Fitness)
        //     .Take((int) Math.Ceiling(results.Count * settings.FittestPercentage))
        //     .Append(bestEverPika)
        //     .OfType<SimulationPika>()
        //     .ToList();
        //
        // // new kids by crossing over and mutations
        // var children = Enumerable.Range(0, settings.GenePoolSize - bestGenomes.Count)
        //     .Select(_ => new
        //     {
        //         Parent1 = bestGenomes[Rnd.Next(bestGenomes.Count)],
        //         Parent2 = bestGenomes[Rnd.Next(bestGenomes.Count)],
        //     })
        //     .Select(x => new EvolutionArgs
        //     {
        //         Parent1 = x.Parent1.Genome,
        //         Parent2 = x.Parent2.Genome,
        //         PreferParent1 = x.Parent1.Fitness > x.Parent2.Fitness,
        //     })
        //     .Select(args => EvolutionService.MakeChildren(_evolutionSettings, args));
        //
        // var newGenomes = bestGenomes
        //     .Select(x => x.Genome)
        //     .Concat(children)
        //     .ToList();
        //
        // // save genomes
        // if (_history.Count - _lastSavedCount > settings.GeneSaveEvery)
        // {
        //     GenomesPool.Save(settings.GenePoolName, newGenomes);
        //     _lastSavedCount = _history.Count;
        // }
        //
        // return newGenomes;

        throw new NotSupportedException();
    }
}
