using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
namespace Neat.Trainer.Simulations.FindNumber;

public class FindNumber2Simulation : ISimulation
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
                Result = Evaluate(x),
            })
            .Sum(x => x.Result) / 10f;

        throw new NotImplementedException();
        // return [new SimulationResult(_brains.Phenotype, fitness)];
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context)
    {
        var neurons = new[]
        {
            // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" }, // bias

            // input number
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "N" },

            // predefined hidden neurons
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, ActivationFunction = "Gaussian" },

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

        var startNeuron = neurons.First(x => x.Type == NeuronType.Input);
        var synapses = neurons
            .Where(x => x.Type == NeuronType.Hidden)
            .Select(x => new Synapse
            {
                InputNeuronId = startNeuron.Id,
                OutputNeuronId = x.Id,
                Weight = 0,
                IsEnabled = true,
                Innovation = context.GetInnovation(startNeuron.Id, x.Id),
            })
            .ToArray();

        var builder = () => new Genotype
        {
            Generation = 0,
            Synapses = synapses,
            Neurons = neurons.ToArray(),
        };

        var genes = Enumerable
            .Range(0, count)
            .ToList(_ => builder());

        return genes;
    }

    private float Evaluate(int goal)
    {
        var inputs = new float[1];
        inputs[0] = goal / 10f;

        var output = _brains!
            .Run(inputs)
            .Select(x => new
            {
                IsGoal = x.Key.Data == goal.ToString(),
                Fitness = x.Value,
            })
            .ToList();

        var goalIndex = output.FindIndex(x => x.IsGoal);
        var result = ActivationFunctions.SoftMax(output.Select(x => x.Fitness).ToArray());
        return result[goalIndex];

        // var output = _brains!
        //     .Run(inputs)
        //     .GroupBy(x => x.Value) // group same activation values
        //     .OrderByDescending(x => x.Key) // order to select group with the highest activation value
        //     .FirstOrDefault()?
        //     .Random().Key; // choose one neuron randomly;
        //
        // if (output == null) return null;
        // if (!int.TryParse(output.Data, out var result)) throw new Exception("Failed to parse output data");
        // return result;
    }
}
