using System.Drawing;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
using Neat.Trainer.Simulations.FieldRunner.Enums;
using Neat.Trainer.Simulations.FieldRunner.Models;
using Neat.Trainer.Simulations.FieldRunner.Services;
namespace Neat.Trainer.Simulations.FieldRunner;

public class FieldRunnerSimulation : ISimulation
{
    private Genotype? _genome;

    public void Initialize(ConcurrentLoop<Genotype> genomes)
    {
        _genome = genomes.GetNext();
    }

    public IReadOnlyCollection<SimulationResult> Run(CancellationToken cancellationToken)
    {
        if (_genome == null)
            throw new InvalidOperationException("Genome is not initialized");

        var pikas = new[]
        {
            new PhenotypeRunner(PhenotypeBuilder.Build(_genome)),
        };

        var worldSize = new Size(50, 50);
        var settings = new WorldSettings
        {
            WorldSize = worldSize,
            InitialFoodCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
            ObstaclesCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
            PoisonsCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
        };

        return new TheWorld(pikas, settings).Simulate(cancellationToken);
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context)
    {
        var neurons = new[]
        {
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" }, // bias

            // food
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food L" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food F" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food R" },

            // poison
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois L" },
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois F" },
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois R" },

            // walls
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall L" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall F" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall R" },

            // additional signals
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "RND" }, // random -1 .. 1
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Sin" }, // sinuses

            // actions
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Left.ToString(), Label = "Move L", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) }, // move left
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Forward.ToString(), Label = "Move F", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) }, // move forward
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Right.ToString(), Label = "Move R", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) }, // move right
        };

        var builder = () => new Genotype
        {
            Generation = 0,
            Synapses = [],
            Neurons = neurons,
        };

        var genes = Enumerable
            .Range(0, count)
            .ToList(_ => builder());

        return genes;
    }
}
