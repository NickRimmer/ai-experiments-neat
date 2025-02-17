using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
namespace Neat.Trainer.Simulations.FindNumber;

public class FindNumberSimulation : ISimulation
{
    private PhenotypeRunner? _brains; // TODO wrong, phenotype should be created for each run, as it is stateful

    public void Initialize(ConcurrentLoop<Genotype> genomes)
    {
        var genome = genomes.GetNext();
        if (!PhenotypeBuilder.TryBuild(genome, out var phenotype)) throw new Exception("Failed to build phenotype");

        _brains = new PhenotypeRunner(phenotype);
    }

    public IReadOnlyCollection<SimulationResult> Run(CancellationToken cancellationToken)
    {
        if (_brains == null) throw new Exception("Simulation is not initialized");

        var fitness = Enumerable.Range(0, 10)
            .Select(x => new
            {
                Goal = x,
                Found = Evaluate(x),
            })
            .Select(x =>
            {
                var fitness = x.Found == x.Goal ? 1f : 0f;
                return new
                {
                    x.Goal,
                    x.Found,
                    Fitness = fitness,
                };
            })
            .Count(x => x.Fitness > 0) / 10f;

        // penalize synapse count
        const float penaltySize = .01f;
        const int allowedSynapses = 10;
        var synapsePenalty = Math.Max(0, (_brains.Phenotype.Genome.Synapses.Count(x => x.IsEnabled) - allowedSynapses) * penaltySize);
        fitness -= synapsePenalty;

        throw new NotImplementedException();
        // return [new SimulationResult(_brains.Phenotype, fitness)];
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context)
    {
        var neurons = new[]
        {
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" }, // bias

            // input number
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N0" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N1" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N2" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N3" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N4" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N5" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N6" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N7" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N8" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N9" },

            // output values
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "0", Label = "0" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "1", Label = "1" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "2", Label = "2" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "3", Label = "3" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "4", Label = "4" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "5", Label = "5" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "6", Label = "6" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "7", Label = "7" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "8", Label = "8" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "9", Label = "9" },
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

    public int Evaluate(int goal)
    {
        var inputs = new float[10];
        inputs[goal] = 1.0f;

        var output = _brains!
            .Run(inputs)
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .FirstOrDefault();

        if (output == null) return -1;
        if (!int.TryParse(output.Data, out var result)) return -1;
        return result;
    }
}
