using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Training;
namespace Neat.Trainer.Simulations.DigitNum;

public class DigitNumSimulation : ISimulation
{
    private static readonly IReadOnlyDictionary<int, int[]> NumericSections = new Dictionary<int, int[]>
    {
        { 0, [0, 1, 2, 3, 4, 5] },
        { 1, [5, 6] },
        { 2, [0, 5, 6, 2, 3] },
        { 3, [0, 5, 6, 4, 3] },
        { 4, [1, 6, 5, 4] },
        { 5, [0, 1, 6, 4, 3] },
        { 6, [0, 1, 2, 3, 4, 6] },
        { 7, [0, 5, 4] },
        { 8, [0, 1, 2, 3, 4, 5, 6] },
        { 9, [6, 1, 0, 5, 4, 3] },
    };

    private Genotype? _genome;

    public void Initialize(ConcurrentLoop<Genotype> genomes)
    {
        _genome = genomes.GetNext();
    }

    public IReadOnlyCollection<SimulationResult> Run(CancellationToken cancellationToken)
    {
        if (_genome == null) throw new Exception("Simulation is not initialized");

        var fitness = Enumerable.Range(0, 10)
            .Select(x => new
            {
                Goal = x,
                Brains = new PhenotypeRunner(PhenotypeBuilder.Build(_genome ?? throw new Exception("Genome is not initialized"))),
            })
            .Select(x =>
            {
                var fitness = Evaluate(x.Goal, x.Brains);

                // penalize hidden neurons, to reduce complexity
                fitness -= x.Brains.Phenotype.Genome.Neurons.Count(neuron => neuron.Type == NeuronType.Hidden) * .001f;

                return new
                {
                    x.Goal,
                    Fitness = fitness,
                };
            })
            .Sum(x => x.Fitness) / 10f;

        return [new SimulationResult(_genome, fitness)];
    }

    public IReadOnlyCollection<Genotype> BuildInitialPopulation(int count, GenomesContext context)
    {
        var neurons = new[]
        {
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" }, // bias

            // sections
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S0" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S1" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S2" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S3" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S4" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S5" },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "S6" },

            // output values
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "0", Label = "0", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "1", Label = "1", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "2", Label = "2", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "3", Label = "3", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "4", Label = "4", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "5", Label = "5", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "6", Label = "6", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "7", Label = "7", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "8", Label = "8", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
            new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = "9", Label = "9", ActivationFunction = nameof(ActivationFunctions.HyperbolicTangent) },
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

    private static float Evaluate(int goal, PhenotypeRunner brains)
    {
        var inputs = new float[7];
        foreach (var i in NumericSections[goal])
            inputs[i] = 1.0f;

        var output = brains.Run(inputs).Where(x => x.Key.Type == NeuronType.Output).ToList();
        var goalIndex = output.FindIndex(x => x.Key.Data?.ToString().Equals(goal.ToString()) == true);
        var softMax = ActivationFunctions.SoftMax(output.Select(x => x.Value).ToList());

        return softMax[goalIndex];
    }
}
