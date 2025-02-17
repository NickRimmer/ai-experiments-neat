using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Neat.Core.Evolution;
using Neat.Core.Genomes;
using Neat.Core.Species;
using Neat.Core.Training;
using Neat.Trainer.Modules.Config;
using Neat.Trainer.Modules.Storage;

#pragma warning disable IL2026
#pragma warning disable IL2072
#pragma warning disable IL2070

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var settings = ConfigService.GetSettings(args.FirstOrDefault() ?? "default");

var serviceCollection = new ServiceCollection()
    .AddSingleton(settings)
    .AddSingleton<SpeciesBuilder>()
    .AddSingleton(settings.Species)
    .AddSingleton<IncubatorService>()
    .AddSingleton<EvolutionService>()
    .AddSingleton(settings.Evolution)
    .AddSingleton<TrainerService>()
    .AddSingleton(settings.Training)
    .AddSingleton<SimulationProvider>()
    .AddSingleton(settings.Simulation)
    .AddSingleton<StorageService>()
    .AddSingleton<GenomesContext>();

// all classes that implement ISimulation
var simulationTypes = typeof(Program)
    .Assembly
    .GetTypes()
    .Where(x => x.GetInterfaces().Contains(typeof(ISimulation)));

foreach (var simulationType in simulationTypes)
{
    // transient for each simulation is important, because they can have internal state
    serviceCollection.AddKeyedTransient(typeof(ISimulation), simulationType.Name, simulationType);
}

// build services
using var services = serviceCollection.BuildServiceProvider();

var trainer = services.GetRequiredService<TrainerService>();
var incubator = services.GetRequiredService<IncubatorService>();
var context = services.GetRequiredService<GenomesContext>();
var speciesBuilder = services.GetRequiredService<SpeciesBuilder>();
using var storage = services.GetRequiredService<StorageService>();

// load simulation data
var genData = storage.ReadGenData();
settings.Species.SpeciesThreshold = genData.SpeciesThreshold;
var iteration = genData.Iteration;

IReadOnlyCollection<Genotype> genomes = genData
    .Species
    .SelectMany(x => x.Genomes)
    .ToList();
context.Rebuild(genomes);
if (genomes.Count == 0) genomes = services.GetRequiredService<SimulationProvider>().BuildInitialPopulation();

var bestFitness = genomes.Max(x => x.HistoricalFitness);

// handle cancelation
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    // ReSharper disable once AccessToDisposedClosure
    cts.Cancel();
    args.Cancel = true;
};

// let's start the rock
Log.Information("Training started");

var sw = Stopwatch.StartNew();
var lastIteration = iteration;
var informEvery = TimeSpan.FromSeconds(5);

while (!cts.IsCancellationRequested)
{
    // run trainer
    iteration += 1;
    var trainedGenomes = trainer.Run(genomes, cts.Token);
    if (trainedGenomes.Count == 0) break; // can be canceled

    // log if fitness improved
    var maxFitness = (float) Math.Round(trainedGenomes.Max(x => x.HistoricalFitness), 2);
    if (bestFitness < maxFitness)
    {
        bestFitness = maxFitness;
        Log.Information("New max fitness: {Fitness}", maxFitness);
    }

    // build new population
    var currentSpecies = speciesBuilder.Build(trainedGenomes);
    var newSpecies = incubator.BuildNewPopulation(currentSpecies, settings.Training.KillRate, settings.Simulation.Population);
    storage.WriteGenomes(iteration, settings.Species.SpeciesThreshold, newSpecies);
    // storage.Flush(true); // TODO temporary
    genomes = newSpecies.SelectMany(x => x.Genomes).ToList();

    // rebuild context time to time, to clean up from removed genomes
    if (iteration % 1_000 == 0) context.Rebuild(genomes);

    // log iteration
    if (sw.Elapsed >= informEvery)
    {
        Log.Information("Iteration {Iteration} | {Time}/s | {CurrentMaxFitness} ({BestFitness}) fitness", iteration, Math.Round((iteration - lastIteration) / sw.Elapsed.TotalSeconds), maxFitness, bestFitness);
        lastIteration = iteration;
        sw.Restart();
    }
}

// make sure all data is saved
storage.Flush(true);
Log.Information("Training finished");
