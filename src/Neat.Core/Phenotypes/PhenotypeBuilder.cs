using System.Diagnostics.CodeAnalysis;
using Neat.Core.Genomes;
namespace Neat.Core.Phenotypes;

public static class PhenotypeBuilder
{
    public static bool TryBuild(Genotype genome, [NotNullWhen(true)] out Phenotype? result)
    {
        if (!IsValidateGenome(genome))
        {
            result = null;
            return false;
        }

        var activations = BuildActivationValues(genome);
        var executionPlan = BuildExecutionPlan(genome);

        result = new Phenotype
        {
            Genome = genome,
            Memory = new float?[genome.Neurons.Length],
            Activations = activations,
            ExecutionPlan = executionPlan,
        };
        return true;
    }

    public static Phenotype Build(Genotype genome) => TryBuild(genome, out var result) ? result : throw new InvalidOperationException("Failed to build phenotype");

    public static (IReadOnlyCollection<Neuron> Neurons, IReadOnlyCollection<Synapse> Synapses) CleanDeadNeurons(IEnumerable<Neuron> neurons, IEnumerable<Synapse> synapses)
    {
        var neuronsList = new List<Neuron>(neurons);
        var synapsesList = new List<Synapse>(synapses.Where(x => x.IsEnabled));

        var lastNeuronsCount = -1;
        while (lastNeuronsCount != neuronsList.Count)
        {
            lastNeuronsCount = neuronsList.Count;

            // filter out dead neurons
            neuronsList = neuronsList
                .Where(neuron =>
                {
                    var hasInput = synapsesList.Any(synapse => synapse.IsEnabled && synapse.OutputNeuronId == neuron.Id);
                    var hasOutput = synapsesList.Any(synapse => synapse.IsEnabled && synapse.InputNeuronId == neuron.Id);

                    return neuron.Type switch
                    {
                        NeuronType.Bias => hasOutput,
                        NeuronType.Input => hasOutput,
                        NeuronType.Hidden => hasInput && hasOutput,
                        NeuronType.Output => hasInput,
                        _ => throw new InvalidOperationException($"Unknown neuron type {neuron.Type}"),
                    };
                })
                .ToList();

            // filter out dead synapses
            synapsesList = synapsesList
                .Where(synapse =>
                    neuronsList.Any(neuron => neuron.Id == synapse.InputNeuronId) &&
                    neuronsList.Any(neuron => neuron.Id == synapse.OutputNeuronId))
                .ToList();
        }

        return (neuronsList, synapsesList);
    }

    private static bool IsValidateGenome(Genotype genome)
    {
        foreach (var neuron in genome.Neurons)
        {
            // no input neurons with any input synapses
            if (neuron.Type is NeuronType.Bias or NeuronType.Input && genome.Synapses.Any(synapse => synapse.OutputNeuronId == neuron.Id))
            {
                Log.Warning("Neuron {NeuronId} is input neuron with income connection", neuron.Id);
                return false;
            }

            // no output neurons with any output synapses
            if (neuron.Type is NeuronType.Output && genome.Synapses.Any(synapse => synapse.InputNeuronId == neuron.Id))
            {
                Log.Warning("Neuron {NeuronId} is output neuron with outcome connection", neuron.Id);
                return false;
            }
        }

        if (genome.Neurons.Count(x => x.Type == NeuronType.Bias) > 1)
        {
            Log.Warning("Genome must have 1 or less bias neurons");
            return false;
        }

        return true;
    }

    private static float[] BuildActivationValues(Genotype genome)
    {
        var inputs = genome
            .Neurons
            .Select(neuron => neuron.Type switch
            {
                NeuronType.Input => 0.0f,
                NeuronType.Bias => 1.0f,
                _ => 0,
            })
            .ToArray();

        return inputs;
    }

    private static ExecutionItem[] BuildExecutionPlan(Genotype genome)
    {
        // find all output neurons
        var outputNeurons = genome
            .Neurons
            .Where(neuron => neuron.Type == NeuronType.Output);

        // create execution plan for each output neuron dependencies and their dependencies and so on, then reverse it
        var executionPlan = outputNeurons
            .Select(neuron => GetInputExecutions(neuron, genome).Reverse()) // reverse to start from input neurons
            .SelectMany(x => x); // flatten

        // distinct by target neuron index, that we will activate each affected neuron only once
        executionPlan = executionPlan
            .GroupBy(x => x.TargetNeuronIndex)
            .Select(x => x.First()); // I want to make sure first item is selected

        // detect recurrent neurons and mark them in execution plan
        executionPlan = executionPlan
            .Select(x => x with
            {
                IsRecurrent = !IsInputNeuron(genome.Neurons[x.TargetNeuronIndex]) && !HasConnectionToInputNeurons(genome.Neurons[x.TargetNeuronIndex].Id, genome),
            });

        // cleanup execution plan from unused neurons
        executionPlan = Cleanup(executionPlan, genome);

        return executionPlan.ToArray();
    }

    private static bool IsInputNeuron(Neuron neuron) => neuron.Type switch
    {
        NeuronType.Bias => true,
        NeuronType.Input => true,
        _ => false,
    };

    private static IEnumerable<ExecutionItem> Cleanup(IEnumerable<ExecutionItem> executionPlan, Genotype genome)
    {
        // remove all dead neurons
        var (neurons, _) = CleanDeadNeurons(genome.Neurons, genome.Synapses);

        var activeNeurons = neurons.ToList(x => x.Id);
        var result = executionPlan

            // remove dead neurons
            .Select(x => new
            {
                ExecutionItem = x,
                Neuron = genome.Neurons[x.TargetNeuronIndex],
            })
            .Where(x => activeNeurons.Contains(x.Neuron.Id))

            // remove dependencies to dead neurons
            .Select(x => x.ExecutionItem with
            {
                Dependencies = x
                    .ExecutionItem
                    .Dependencies
                    .Where(dep => activeNeurons.Contains(genome.Neurons[dep.ActivationIndex].Id))
                    .ToArray(),
            });

        return result;
    }

    private static IEnumerable<ExecutionItem> GetInputExecutions(Neuron neuron, Genotype genome, HashSet<Guid>? visited = null)
    {
        visited ??= new HashSet<Guid>();
        if (!visited.Add(neuron.Id)) yield break; // avoid loops

        if (!genome.Neurons.TryFindIndex(x => x.Id == neuron.Id, out var targetNeuronIndex))
            throw new InvalidOperationException($"Cannot find {neuron.Type} neuron with id {neuron.Id} in genotype");

        // find all synapses that target to the neuron
        var inputSynapses = genome
            .Synapses
            .Where(synapse => synapse.IsEnabled && synapse.OutputNeuronId == neuron.Id)
            .ToList();

        if (inputSynapses.Count == 0) yield break; // no dependencies, nothing to execute

        // build dependencies for the neuron
        var dependencies = inputSynapses
            .Select(synapse => new ExecutionDependency
            {
                Weight = synapse.Weight,
                ActivationIndex = genome.Neurons.TryFindIndex(x => x.Id == synapse.InputNeuronId, out var outputNeuronIndex)
                    ? outputNeuronIndex.Value
                    : throw new InvalidOperationException($"Cannot find input neuron with id {synapse.InputNeuronId} for synapse {synapse.Innovation}"),
            })
            .ToArray();

        // return current neuron execution item
        yield return new ExecutionItem
        {
            TargetNeuronIndex = targetNeuronIndex.Value,
            Dependencies = dependencies,
            ActivationFunction = ActivationFunctions.GetFunction(neuron.ActivationFunction),
        };

        // find sub-dependencies for the neuron
        var inputNeuronDependencies = inputSynapses
            .Select(synapse =>
                genome.Neurons.FirstOrDefault(gen => gen.Id == synapse.InputNeuronId) ??
                throw new InvalidOperationException($"Cannot find input neuron with id {synapse.InputNeuronId} for synapse {synapse.Innovation}"))
            .SelectMany(x => GetInputExecutions(x, genome, visited));

        foreach (var x in inputNeuronDependencies)
            yield return x;
    }

    private static bool HasConnectionToInputNeurons(Guid neuroneId, Genotype genome, HashSet<Guid>? visited = null)
    {
        visited ??= new HashSet<Guid>();
        if (!visited.Add(neuroneId)) return false; // loop detected, path is not connected to input neurons

        var incomingSynapses = genome
            .Synapses
            .Where(x => x.OutputNeuronId == neuroneId)
            .ToList();

        var connectedToInputNeuron = incomingSynapses
            .Select(x => genome.Neurons.FirstOrDefault(n => n.Id == x.InputNeuronId)
                ?? throw new InvalidOperationException($"Cannot find input neuron with id {x.InputNeuronId} for synapse {x.Innovation}"))
            .Any(x => x.Type is NeuronType.Input or NeuronType.Bias);

        if (connectedToInputNeuron) return true;

        return incomingSynapses
            .Select(x => HasConnectionToInputNeurons(x.InputNeuronId, genome, visited))
            .Any(x => x);
    }
}
