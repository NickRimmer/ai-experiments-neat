using System.Diagnostics.CodeAnalysis;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
namespace Neat.Trainer.Simulations.Xor;

public class XorSimulation : ISimulation
{
    private readonly Data[] _intpus;
    private Genotype? _genome;

    public XorSimulation()
    {
        _intpus =
        [
            new (0, 0, 0), // expected 0
            new (0, 1, 1), // expected 1
            new (1, 0, 1), // expected 1
            new (1, 1, 0), // expected 0
        ];
    }

    public void Initialize(ConcurrentLoop<Genotype> genomes) => _genome = genomes.GetNext();

    public IReadOnlyCollection<SimulationResult> Run(CancellationToken cancellationToken)
    {
        if (_genome == null) throw new Exception("Simulation is not initialized");

        var sims = _intpus
            .Select(x => new
            {
                Input = x,
                Output = new PhenotypeRunner(PhenotypeBuilder.Build(_genome)) // create new phenotype for each run, it is important as phenotype is stateful
                    .Run([x.A, x.B]).Single(r => r.Key.Type == NeuronType.Output).Value,
            })
            .Select(x => new
            {
                x.Input,
                x.Output,
                Fitness = 1 - Math.Abs(x.Input.Expected - x.Output),
            })
            .ToList();

        var fitness = sims.Sum(x => x.Fitness) / 4f;
        return [new SimulationResult(_genome, fitness)];
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context)
    {
        // two inputs and one output
        var neurons = new[]
        {
            // input
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "A" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "B" },

            // hidden
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },

            // output
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Label = "Result", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
        };

        var outputNeuron = neurons.Single(x => x.Type == NeuronType.Output);
        var synapses = neurons
            .Where(x => x.Type == NeuronType.Hidden)
            .Select(neuron => new Synapse
            {
                InputNeuronId = neuron.Id,
                OutputNeuronId = outputNeuron.Id,
                Innovation = context.GetInnovation(neuron.Id, outputNeuron.Id),
                Weight = 0,
                IsEnabled = true,
            })
            .ToArray();

        var result = count.Times(() => new Genotype
        {
            Neurons = neurons,
            Synapses = synapses,
        }).ToList();

        return result;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
    private record Data(float A, float B, float Expected);
}
