using System.Diagnostics;
using Neat.Core.Genomes;
namespace Neat.Core.Species;

public class SpeciesBuilder
{
    private const float InitialThreshold = 3f;

    // efficient threshold values are between 1.5 and 3
    private const float MinThreshold = .5f;
    private const float MaxThreshold = 4f;

    private readonly SpeciesSettings _settings;

    public SpeciesBuilder(SpeciesSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlyCollection<Specie> Build(IReadOnlyCollection<Genotype> genomes)
    {
        // group genomes by distance
        var groups = new List<List<Genotype>>();
        var speciesThreshold = _settings.SpeciesThreshold is null or <= 0
            ? InitialThreshold
            : _settings.SpeciesThreshold;

        foreach (var genome in genomes)
        {
            var target = groups
                .Select(x => new
                {
                    Group = x,
                    Distance = CalcSpecieDistance(genome, x, _settings),
                })
                .Where(x => x.Distance < speciesThreshold)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            // // skip clones, to avoid local maximas
            // if (target != null && target.Distance < 0.001)
            //     continue;

            var targetGroup = target?.Group;
            if (targetGroup == null)
            {
                targetGroup = new List<Genotype>();
                groups.Add(targetGroup);
            }

            targetGroup.Add(genome);
        }

        // create species
        var species = groups
            .Select(group => new Specie
            {
                Genomes = group,
            })
            .ToList();

        // adjust distance threshold
        if (species.Count > _settings.SpeciesTargetCount) speciesThreshold += _settings.DistanceThresholdAdjustmentRate;
        else if (species.Count < _settings.SpeciesTargetCount) speciesThreshold -= _settings.DistanceThresholdAdjustmentRate;

        // when too many species, increase threshold even more
        if (species.Count > _settings.SpeciesTargetCount * 2) speciesThreshold += _settings.DistanceThresholdAdjustmentRate;
        else speciesThreshold = Math.Clamp(speciesThreshold.Value, MinThreshold, MaxThreshold);

        _settings.SpeciesThreshold = speciesThreshold;
        return species;
    }

    private static float CalcGenomesDistance(Genotype genome1, Genotype genome2, SpeciesSettings settings)
    {
        var allSynapses = genome1
            .Synapses
            .Concat(genome2.Synapses)
            .ToList();

        // find last innovation of smaller genome
        var lastInnovationInSharedRange = Math.Min(
            genome1.Synapses.DefaultIfEmpty().Max(x => x?.Innovation ?? 0),
            genome2.Synapses.DefaultIfEmpty().Max(x => x?.Innovation ?? 0));

        var groupedSynapses = allSynapses
            .Where(x => x.IsEnabled) // only enabled synapses used for distance calculation
            .GroupBy(x => x.Innovation)
            .ToList();

        // just to catch any unexpected synapse count
        Debug.Assert(groupedSynapses.All(x => x.Count() is >= 1 and <= 2));

        // average weight of matched synapses
        var averageWeightDiff = groupedSynapses
            .Where(x => x.Count() == 2)
            .Select(x => Math.Abs(x.ElementAt(0).Weight - x.ElementAt(1).Weight))
            .DefaultIfEmpty(0)
            .Average();

        // unmatched synapses
        var (disjointCount, excessCount) = groupedSynapses
            .Where(x => x.Count() == 1)
            .Select(x => x.First())
            .Aggregate(
                (Disjoint: 0, Excess: 0),
                (acc, x) => x.Innovation <= lastInnovationInSharedRange
                    ? (acc.Disjoint + 1, acc.Excess)
                    : (acc.Disjoint, acc.Excess + 1));

        var activeSynapses = groupedSynapses.Select(x => x.First()).ToArray();
        var matchedNeurons = genome1
            .Neurons
            .Concat(genome2.Neurons)
            // .Where(genome => groupedSynapses.Any(x => x.Any(synapse => synapse.OutputNeuronId == genome.Id || synapse.InputNeuronId == genome.Id))) // only neurons connected to active synapses
            .GroupBy(x => x.Id)
            .Where(x => x.Count() > 1 && activeSynapses.Any(synapse => synapse.OutputNeuronId == x.Key || synapse.InputNeuronId == x.Key))
            .ToList(x => x.ToArray());

        // add diff for each activation mismatch
        var activationFunctionDiff = matchedNeurons.Count == 0 ? 0 : matchedNeurons
            .Sum(x => x[0].ActivationFunction.Equals(x[1].ActivationFunction, StringComparison.Ordinal) ? 0 : 1) / (float) matchedNeurons.Count;

        var biasDiff = matchedNeurons.Count == 0 ? 0 : matchedNeurons
            .Sum(x => Math.Abs(x[0].Bias - x[1].Bias)) / matchedNeurons.Count;

        var distance =
            (settings.ExcessCoefficient * excessCount / settings.NormalizationFactor) +
            (settings.DisjointCoefficient * disjointCount / settings.NormalizationFactor) +
            (settings.ActivationDiffCoefficient * activationFunctionDiff / settings.NormalizationFactor) +
            (settings.BiasDiffCoefficient * biasDiff / settings.NormalizationFactor) +
            (settings.WeightCoefficient * averageWeightDiff);

        return distance;
    }

    private static float CalcSpecieDistance(Genotype genome, IEnumerable<Genotype> group, SpeciesSettings settings)
    {
        Func<float> fn = settings.DistanceFunction switch
        {
            SpeciesSettings.SpecieDistanceFunction.MinDistanceToAllGenomes => () =>
            {
                return group.Min(x => CalcGenomesDistance(genome, x, settings));
            },

            SpeciesSettings.SpecieDistanceFunction.DistanceToRandomGenome => () =>
            {
                var specieGenome = group.Random();
                return CalcGenomesDistance(genome, specieGenome, settings);
            },

            SpeciesSettings.SpecieDistanceFunction.DistanceToHalfRandomGenomes => () =>
            {
                var list = group.ToList();
                return list
                    .Shuffle()
                    .Take(list.Count / 2)
                    .Min(x => CalcGenomesDistance(genome, x, settings));
            },

            _ => throw new ArgumentOutOfRangeException(),
        };

        return fn.Invoke();
    }
}
