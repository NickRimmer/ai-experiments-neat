using Neat.Core.Genomes;
namespace Neat.Core.Phenotypes;

public class PhenotypeRunner
{
    public Guid Id { get; } = Guid.NewGuid();

    private readonly int[] _inputsMap;
    private readonly int? _biasIndex;
    private readonly int[] _outputsMap;

    public PhenotypeRunner(Phenotype phenotype)
    {
        Phenotype = phenotype ?? throw new ArgumentNullException(nameof(phenotype));
        _biasIndex = FindIndexes(NeuronType.Bias).SingleOrDefault(-1); // default for int is 0 what can be a valid index
        if (_biasIndex == -1) _biasIndex = null;

        _inputsMap = FindIndexes(NeuronType.Input);
        _outputsMap = FindIndexes(NeuronType.Output);
    }

    public Phenotype Phenotype { get; }

    public Dictionary<Neuron, float> Run(float[] inputs)
    {
        if (inputs.Length != _inputsMap.Length)
            throw new ArgumentException($"Input length does not match the number of input neurons ({inputs.Length} != {_inputsMap.Length})");

        // reset input activations
        Phenotype.Activations = new float[Phenotype.Genome.Neurons.Length];

        if (_biasIndex.HasValue) Phenotype.Activations[_biasIndex.Value] = 1.0f;

        for (var i = 0; i < inputs.Length; i++)
            Phenotype.Activations[_inputsMap[i]] = inputs[i];

        // move memory to activations
        for (var i = 0; i < Phenotype.Memory.Length; i++)
        {
            var value = Phenotype.Memory[i];
            if (!value.HasValue) continue;

            Phenotype.Activations[i] = value.Value;
            Phenotype.Memory[i] = null;
        }

        // run execution plan
        foreach (var execution in Phenotype.ExecutionPlan)
        {
            var inputSum = execution
                .Dependencies
                .Select(dependency => Phenotype.Activations[dependency.ActivationIndex] * dependency.Weight)
                .Sum();

            var activation = execution.ActivationFunction(inputSum, Phenotype.Genome.Neurons[execution.TargetNeuronIndex].Bias);
            if (execution.IsRecurrent)
            {
                Phenotype.Memory[execution.TargetNeuronIndex] = activation;
            }
            else
            {
                Phenotype.Activations[execution.TargetNeuronIndex] = activation;
            }
        }

        // return output with activations
        return _outputsMap
            .Select(index => (Neuron: Phenotype.Genome.Neurons[index], Activation: Phenotype.Activations[index]))
            .ToDictionary(x => x.Neuron, x => x.Activation);
    }

    private int[] FindIndexes(NeuronType type) => Phenotype
        .Genome
        .Neurons
        .Select((neuron, index) => neuron.Type == type ? (int?) index : null)
        .OfType<int>()
        .ToArray();
}
