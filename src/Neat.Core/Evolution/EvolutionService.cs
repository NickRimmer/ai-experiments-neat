using System.Diagnostics.CodeAnalysis;
using Neat.Core.Genomes;
namespace Neat.Core.Evolution;

public class EvolutionService
{
    private const float SynapseWeightMutationPower = .5f;
    private const float BiasMutationPower = 1f;

    private readonly GenomesContext _context;
    private readonly EvolutionSettings _settings;

    public EvolutionService(GenomesContext context, EvolutionSettings settings)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Genotype MakeChildren(EvolutionArgs args)
    {
        try
        {
            // crossover and mutate
            var child = Crossover(args);
            child = Mutate(child, _settings, _context);

            return child;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static Genotype Crossover(EvolutionArgs args)
    {
        // build synapses
        var synapses1 = args.Parent1
            .Synapses
            .Select(synapse => new
            {
                synapse.Innovation,
                Synapse = synapse,
                Preferred = args.PreferParent1,
            })
            .ToHashSet();

        var synapses2 = args.Parent2
            .Synapses
            .Select(synapse => new
            {
                synapse.Innovation,
                Synapse = synapse,
                Preferred = !args.PreferParent1,
            })
            .ToHashSet();

        // merge synapses, find max innovation of smaller parent
        var lastInnovationInSharedRange = Math.Min(
            synapses1.DefaultIfEmpty().Max(x => x?.Innovation ?? 0),
            synapses2.DefaultIfEmpty().Max(x => x?.Innovation ?? 0));

        var synapses = synapses1
            .Concat(synapses2)
            .GroupBy(x => x.Innovation)
            .Select(x =>
            {
                // when duplicates, select random
                if (x.Count() > 1) return x.Shuffle().First().Synapse;
                var synapse = x.First();

                // when disjoint, always copy
                if (synapse.Innovation <= lastInnovationInSharedRange) return synapse.Synapse;

                // when excess, copy only when preferred
                if (synapse.Preferred) return synapse.Synapse;

                return null;
            })
            .OfType<Synapse>()
            .ToArray();

        // copy required neurons
        var allNeurons = args
            .Parent1
            .Neurons
            .Concat(args.Parent2.Neurons)
            .DistinctBy(x => x.Id)
            .ToHashSet();

        var neurons = allNeurons
            .Where(neuron =>
                synapses.Any(synapse => synapse.InputNeuronId == neuron.Id || synapse.OutputNeuronId == neuron.Id) ||
                neuron.Type is NeuronType.Bias or NeuronType.Input or NeuronType.Output) // keep required
            .ToArray();

        // filter neurons to stay only required or with any synapse
        neurons = neurons
            .Where(neuron =>
                neuron.Type is NeuronType.Bias or NeuronType.Input or NeuronType.Output ||
                synapses.Any(synapse => synapse.InputNeuronId == neuron.Id || synapse.OutputNeuronId == neuron.Id))
            .ToArray();

        return new Genotype
        {
            Neurons = neurons,
            Synapses = synapses,
            Generation = (args.PreferParent1 ? args.Parent1.Generation : args.Parent2.Generation) + 1,
            HistoricalFitness = (args.Parent1.HistoricalFitness + args.Parent2.HistoricalFitness) / 2f,
        };
    }

    private static Genotype Mutate(Genotype child, EvolutionSettings settings, GenomesContext context)
    {
        var result = child;
        IEnumerable<Mutation> mutations =
        [
            // non-structural mutations
            new (() => ModifySynapseWeight(child, settings), child.Synapses.Count(x => x.IsEnabled) > 0 ? settings.NonStructSynapseModifyProbability : 0),
            new (() => ReplaceSynapseWeight(child, settings), child.Synapses.Count(x => x.IsEnabled) > 0 ? settings.NonStructSynapseReplaceProbability : 0),
            new (() => ReplaceNeuronActivationFunction(child, settings), child.Neurons.Count(x => x.Type == NeuronType.Hidden) > 0 ? settings.NonStructNeuronActivationReplaceProbability : 0),
            new (() => ModifyNeuronBias(child), child.Neurons.Count(x => x.Type == NeuronType.Hidden) > 0 ? settings.NonStructNeuronBiasProbability : 0),

            // structural mutations
            new (() => AddSynapse(child, settings, context), settings.StructAddSynapsesProbability),
            new (() => AddDirectSynapse(child, settings, context), settings.StructAddDirectSynapsesProbability),
            new (() => DisableSynapse(child), child.Synapses.Count(x => x.IsEnabled) > 0 ? settings.StructDisableSynapsesProbability : 0),
            new (() => EnableSynapse(child), child.Synapses.Count(x => !x.IsEnabled) > 0 ? settings.StructEnableSynapsesProbability : 0),
            new (() => ToggleSynapse(child), child.Synapses.Length > 0 ? settings.StructToggleSynapsesProbability : 0),
            new (() => AddHiddenNeuron(child, context, settings), settings.MaximumHiddenNeurons.HasValue && child.Neurons.Count(x => x.Type == NeuronType.Hidden) >= settings.MaximumHiddenNeurons
                ? 0 // do not add hidden neuron if limit reached
                : child.Synapses.Count(x => x.IsEnabled) > 0
                    ? settings.StructNeuronAddProbability
                    : 0),
            new (() => RemoveHiddenNeuron(child), child.Neurons.Count(x => x.Type == NeuronType.Hidden) > 0 ? settings.StructNeuronRemoveProbability : 0),
        ];

        foreach (var mutation in mutations)
        {
            if (Random.Shared.NextDouble() < mutation.Probability)
                result = mutation.Apply();
        }

        return result;
    }

    private static Genotype AddSynapse(Genotype child, EvolutionSettings settings, GenomesContext context)
    {
        // find neurons with synapses
        var neurons = child
            .Neurons
            .Where(x => x.Type is not NeuronType.Output)
            .Shuffle()
            .ToList();

        foreach (var neuron in neurons)
        {
            // find target neuron
            var targetNeuron = child
                .Neurons
                .Where(x => x.Type is not NeuronType.Input and not NeuronType.Bias)
                .Shuffle()
                .FirstOrDefault(x => !child.Synapses.Any(synapse => synapse.InputNeuronId == neuron.Id && synapse.OutputNeuronId == x.Id));

            // check recursion
            if (targetNeuron != null && !settings.AllowRecurrent && IsInLoop(child, neuron.Id, [targetNeuron.Id])) continue;

            // create or restore synapse
            if (targetNeuron != null)
            {
                // if synapse already exists enable it if disabled
                // var existingSynapse = child.Synapses.FirstOrDefault(x => x.InputNeuronId == neuron.Id && x.OutputNeuronId == targetNeuron.Id);
                // if (existingSynapse != null)
                // {
                //     if (!existingSynapse.IsEnabled)
                //     {
                //         return child with
                //         {
                //             Synapses = child
                //                 .Synapses
                //                 .Select(x => x.Innovation == existingSynapse.Innovation
                //                     ? x with { IsEnabled = true }
                //                     : x)
                //                 .ToArray(),
                //         };
                //     }
                //
                //     // if synapse already exists and enabled, do not change anything
                //     return child;
                // }

                return child with
                {
                    Synapses =
                    [
                        ..child.Synapses, new Synapse
                        {
                            Innovation = context.GetInnovation(neuron.Id, targetNeuron.Id),
                            Weight = GetRandomWeight(settings),
                            IsEnabled = true,
                            InputNeuronId = neuron.Id,
                            OutputNeuronId = targetNeuron.Id,
                        },
                    ],
                };
            }
        }

        return child;
    }

    private static Genotype AddDirectSynapse(Genotype child, EvolutionSettings settings, GenomesContext context)
    {
        // find neurons with synapses
        var neurons = child
            .Neurons
            .Where(x => x.Type is NeuronType.Input or NeuronType.Bias)
            .Shuffle()
            .ToList();

        foreach (var neuron in neurons)
        {
            // find target neuron
            var targetNeuron = child
                .Neurons
                .Where(x => x.Type is NeuronType.Output)
                .Shuffle()
                .FirstOrDefault(x =>!child.Synapses.Any(synapse => synapse.InputNeuronId == neuron.Id && synapse.OutputNeuronId == x.Id));

            // create or restore synapse
            if (targetNeuron != null)
            {
                // if synapse already exists enable it if disabled
                var existingSynapse = child.Synapses.FirstOrDefault(x => x.InputNeuronId == neuron.Id && x.OutputNeuronId == targetNeuron.Id);
                if (existingSynapse != null)
                {
                    if (!existingSynapse.IsEnabled)
                    {
                        return child with
                        {
                            Synapses = child
                                .Synapses
                                .Select(x => x.Innovation == existingSynapse.Innovation
                                    ? x with { IsEnabled = true }
                                    : x)
                                .ToArray(),
                        };
                    }

                    // if synapse already exists and enabled, do not change anything
                    return child;
                }

                return child with
                {
                    Synapses =
                    [
                        ..child.Synapses, new Synapse
                        {
                            Innovation = context.GetInnovation(neuron.Id, targetNeuron.Id),
                            Weight = GetRandomWeight(settings),
                            IsEnabled = true,
                            InputNeuronId = neuron.Id,
                            OutputNeuronId = targetNeuron.Id,
                        },
                    ],
                };
            }
        }

        return child;
    }

    private static bool IsInLoop(Genotype child, Guid source, Guid[] visited)
    {
        if (visited.Contains(source)) return true;
        visited = [..visited, source];

        // check dependencies
        var synapses = child.Synapses.Where(x => x.OutputNeuronId == source).ToArray();
        if (synapses.Length == 0) return false;

        return synapses.Any(x => IsInLoop(child, x.InputNeuronId, visited));
    }

    private static Genotype ToggleSynapse(Genotype child)
    {
        if (child.Synapses.Length == 0) return child;

        var index = Random.Shared.Next(child.Synapses.Length);
        child.Synapses[index] = child.Synapses[index] with
        {
            IsEnabled = !child.Synapses[index].IsEnabled,
        };

        return child;
    }

    private static Genotype DisableSynapse(Genotype child)
    {
        if (child.Synapses.Length == 0) return child;

        var candidate = child.Synapses.Where(x => x.IsEnabled).Shuffle().FirstOrDefault();
        if (candidate == null) return child;

        if (!child.Synapses.TryFindIndex(x => x.Innovation == candidate.Innovation, out var index))
            throw new InvalidOperationException("Synapse not found.");

        child.Synapses[index.Value] = child.Synapses[index.Value] with
        {
            IsEnabled = false,
        };

        return child;
    }

    private static Genotype EnableSynapse(Genotype child)
    {
        if (child.Synapses.Length == 0) return child;

        var candidate = child.Synapses.Where(x => !x.IsEnabled).Shuffle().FirstOrDefault();
        if (candidate == null) return child;

        if (!child.Synapses.TryFindIndex(x => x.Innovation == candidate.Innovation, out var index))
            throw new InvalidOperationException("Synapse not found.");

        child.Synapses[index.Value] = child.Synapses[index.Value] with
        {
            IsEnabled = true,
        };

        return child;
    }

    private static Genotype AddHiddenNeuron(Genotype child, GenomesContext context, EvolutionSettings settings)
    {
        if (child.Synapses.Length == 0) return child;

        var neuron = new Neuron
        {
            Id = Guid.NewGuid(),
            Type = NeuronType.Hidden,
            ActivationFunction = ActivationFunctions.GetRandomFunction(settings.OverrideActivationProbabilities),
        };

        // search victim synapse
        var enabledSynapses = child.Synapses.Where(x => x.IsEnabled).ToList();
        var candidates = enabledSynapses
            .Select(x => new
            {
                Synapse = x,
                LowPriority =
                    child.Neurons.Any(n => n.Id == x.InputNeuronId && n.Type is NeuronType.Hidden or NeuronType.Bias) ||
                    child.Neurons.Any(n => n.Id == x.OutputNeuronId && n.Type is NeuronType.Hidden or NeuronType.Bias),
            })
            .OrderBy(x => x.LowPriority ? 1 : 0) // prefer not connected to hidden or bias
            .Take(Random.Shared.Next(enabledSynapses.Count) + 1)
            .Shuffle()
            .ToList(x => x.Synapse);

        if (candidates.Count == 0) return child;
        var synapse = candidates[0];

        // split synapse
        var newSynapse1 = new Synapse
        {
            Innovation = context.GetInnovation(synapse.InputNeuronId, neuron.Id),
            Weight = 1.0f,
            IsEnabled = true,
            InputNeuronId = synapse.InputNeuronId,
            OutputNeuronId = neuron.Id,
        };

        var newSynapse2 = new Synapse
        {
            Innovation = context.GetInnovation(neuron.Id, synapse.OutputNeuronId),
            Weight = synapse.Weight,
            IsEnabled = true,
            InputNeuronId = neuron.Id,
            OutputNeuronId = synapse.OutputNeuronId,
        };

        // update child
        var synapses = child
            .Synapses
            .Select(x => x.Innovation == synapse.Innovation ? x with { IsEnabled = false } : x) // disable old one
            .Concat([newSynapse1, newSynapse2])
            .ToArray();

        var neurons = child.Neurons.Append(neuron).ToArray();

        return child with
        {
            Synapses = synapses,
            Neurons = neurons,
        };
    }

    private static Genotype RemoveHiddenNeuron(Genotype child)
    {
        var neurone = child
            .Neurons
            .Where(x => x.Type == NeuronType.Hidden)
            .Shuffle()
            .FirstOrDefault();

        if (neurone == null) return child;

        var neurons = child
            .Neurons
            .Where(x => x.Id != neurone.Id)
            .ToArray();

        var synapses = child
            .Synapses
            .Where(x => x.InputNeuronId != neurone.Id && x.OutputNeuronId != neurone.Id)
            .ToArray();

        return child with
        {
            Neurons = neurons,
            Synapses = synapses,
        };
    }

    private static Genotype ModifySynapseWeight(Genotype child, EvolutionSettings settings)
    {
        if (child.Synapses.Length == 0) return child;

        var synapse = child.Synapses.Where(x => x.IsEnabled).RandomOrDefault();
        if (synapse == null) return child;

        var mutationAmount = (float) ((Random.Shared.NextDouble() * 2) - 1) * SynapseWeightMutationPower;
        var weight = Math.Clamp(synapse.Weight + mutationAmount, settings.SynapseWeightRange.Min, settings.SynapseWeightRange.Max);
        if (Math.Abs(weight - synapse.Weight) < float.Epsilon) // if hit the boundary, try opposite
            weight = Math.Clamp(synapse.Weight + (-1 * mutationAmount), settings.SynapseWeightRange.Min, settings.SynapseWeightRange.Max);

        return child with
        {
            Synapses = child
                .Synapses
                .Select(x => x.Innovation == synapse.Innovation
                    ? x with { Weight = weight }
                    : x)
                .ToArray(),
        };
    }

    private static Genotype ReplaceSynapseWeight(Genotype child, EvolutionSettings settings)
    {
        if (child.Synapses.Length == 0) return child;

        var synapse = child.Synapses.Where(x => x.IsEnabled).RandomOrDefault();
        if (synapse == null) return child;

        var weight = GetRandomWeight(settings);
        return child with
        {
            Synapses = child
                .Synapses
                .Select(x => x.Innovation == synapse.Innovation
                    ? x with { Weight = weight }
                    : x)
                .ToArray(),
        };
    }

    private static Genotype ReplaceNeuronActivationFunction(Genotype child, EvolutionSettings settings)
    {
        var hiddenNeurons = child.Neurons.Where(x => x.Type == NeuronType.Hidden).ToList();
        if (hiddenNeurons.Count == 0) return child;

        var index = Random.Shared.Next(hiddenNeurons.Count);
        var neuron = hiddenNeurons[index];

        var activationFunction = ActivationFunctions.GetRandomFunction(settings.OverrideActivationProbabilities);
        var neurons = child.Neurons.Select(x => x.Id == neuron.Id ? x with { ActivationFunction = activationFunction } : x).ToArray();

        return child with { Neurons = neurons };
    }

    private static Genotype ModifyNeuronBias(Genotype child)
    {
        var hiddenNeurons = child.Neurons.Where(x => x.Type == NeuronType.Hidden).ToList();
        if (hiddenNeurons.Count == 0) return child;

        var index = Random.Shared.Next(hiddenNeurons.Count);
        var neuron = hiddenNeurons[index];

        var bias = neuron.Bias + ((float) ((Random.Shared.NextDouble() * 2) - 1) * BiasMutationPower);
        var neurons = child.Neurons.Select(x => x.Id == neuron.Id ? x with { Bias = bias } : x).ToArray();

        return child with { Neurons = neurons };
    }

    private static float GetRandomWeight(EvolutionSettings settings)
    {
        var allowed = settings.SynapseWeightRange.Max - settings.SynapseWeightRange.Min;
        return (float) (Random.Shared.NextDouble() * allowed) + settings.SynapseWeightRange.Min;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
    private record Mutation(Func<Genotype> Apply, float Probability);
}
