using System.Diagnostics;
using Neat.Core.Species;
namespace Neat.Core.Evolution;

public class IncubatorService
{
    private readonly EvolutionService _evolutionService;
    public IncubatorService(EvolutionService evolutionService)
    {
        _evolutionService = evolutionService ?? throw new ArgumentNullException(nameof(evolutionService));
    }

    public IReadOnlyCollection<Specie> BuildNewPopulation(IReadOnlyCollection<Specie> sourceSpecies, float killRate, int targetCount)
    {
        // cull the weakest genomes (survival selection)
        var culledSpecies = CullSpecies(sourceSpecies, killRate);
        var culledCount = culledSpecies.Sum(x => x.Genomes.Count);
        if (culledCount == targetCount) return culledSpecies;
        Debug.Assert(culledCount <= targetCount);

        // merge when we have too many low species but distance between them is too high
        // if (_speciesSettings != null && culledSpecies.Count > _speciesSettings.SpeciesTargetCount && _speciesSettings.SpeciesThreshold > 3)
        // {
        //     const float mergeThreshold = 1.05f; // 5% the lowest fitness species will be merged
        //     var lowValue = culledSpecies.Min(x => x.AverageFitness);
        //     culledSpecies = culledSpecies
        //         .OrderByDescending(x => x.AverageFitness)
        //         .Aggregate(new List<Specie>(), (acc, x) =>
        //         {
        //             // if species close to the lowest fitness, merge it with previous one
        //             if (x.AverageFitness < lowValue * mergeThreshold)
        //             {
        //                 if (acc.Count == 0)
        //                 {
        //                     acc.Add(x);
        //                     return acc;
        //                 }
        //
        //                 acc[^1] = acc[^1] with
        //                 {
        //                     Genomes = Enumerable.DistinctBy([..acc[^1].Genomes, ..x.Genomes], genome => genome.Id).ToList(),
        //                 };
        //             }
        //             else
        //             {
        //                 acc.Add(x);
        //             }
        //
        //             return acc;
        //         });
        // }

        // assign offspring count to species based on fitness
        var speciesOffspring = CountSpeciesOffspring(culledSpecies, targetCount);

        // generate offspring for each species
        return speciesOffspring
            .Select(x =>
            {
                var specie = x.Specie;
                if (x.OffspringCount <= specie.Genomes.Count)
                {
                    return specie with
                    {
                        Genomes = specie
                            .Genomes
                            .OrderByDescending(y => y.HistoricalFitness)
                            .ThenBy(_ => Random.Shared.NextDouble()) // to shuffle genomes with same fitness
                            .Take(x.OffspringCount) // sometimes we can have more genomes than needed, simply take the best ones
                            .ToList(),
                    }; // enough genomes, return as is
                }

                var children = (x.OffspringCount - specie.Genomes.Count)
                    .Times(() =>
                    {
                        var parent1 = specie.Genomes.Random();
                        var parent2 = specie.Genomes.Count == 1
                            ? specie.Genomes.Random() // allow self-reproduction if no other genomes
                            : specie.Genomes.Except([parent1]).Random();

                        var child = _evolutionService.MakeChildren(new ()
                        {
                            Parent1 = parent1,
                            Parent2 = parent2,
                            PreferParent1 = parent1.HistoricalFitness > parent2.HistoricalFitness,
                        });

                        return child;
                    });

                return specie with
                {
                    Genomes = [..children, ..specie.Genomes],
                };
            })
            .ToList();
    }

    private static List<SpecieWithOffspring> CountSpeciesOffspring(IReadOnlyCollection<Specie> species, int targetCount)
    {
        var allSpeciesFitness = species.Sum(x => x.AverageFitness);

        // edge case: in very beginning, it is just one species with all genomes
        if (allSpeciesFitness < 0.0001)
        {
            var allGenomes = species.SelectMany(x => x.Genomes).ToArray();
            return
            [
                new SpecieWithOffspring
                {
                    Specie = new Specie
                    {
                        Genomes = allGenomes,
                    },
                    OffspringCount = targetCount,
                },
            ];
        }

        var result = species
            .Select(specie => new SpecieWithOffspring
            {
                Specie = specie,
                OffspringCount = (int) Math.Round(specie.AverageFitness / allSpeciesFitness * targetCount),
            })
            .Where(x => x.OffspringCount > 0)
            .ToList();

        // fix rounding issues (ensure total count matches target)
        var diff = targetCount - result.Sum(x => x.OffspringCount);
        foreach (var entry in result.Shuffle().Take(Math.Abs(diff)))
            entry.OffspringCount += Math.Sign(diff);

        Debug.Assert(result.Sum(x => x.OffspringCount) == targetCount);
        return result
            .Where(x => x.OffspringCount > 0)
            .ToList();
    }

    private static List<Specie> CullSpecies(IReadOnlyCollection<Specie> species, float killRate)
    {
        var bestFitness = species.Max(x => x.Genomes.Max(y => y.HistoricalFitness));
        var worstFitness = species.Min(x => x.Genomes.Min(y => y.HistoricalFitness));

        var result = species
            .Select(specie => specie with
            {
                Genomes = specie.Genomes
                    .OrderByDescending(x => x.HistoricalFitness)
                    .ThenBy(_ => Random.Shared.NextDouble()) // to shuffle genomes with same fitness
                    // .Take((int) Math.Round(specie.Genomes.Count * (1f - killRate))) // Keep only best x%
                    .Take(Math.Max(1, (int) Math.Round(specie.Genomes.Count * (1f - killRate)))) // Keep only best x%
                    .ToList(),
            })
            .Where(x => x.Genomes.Count > 0) // remove empty species

            // .Where(x => x.Genomes.Count > 1 || x.Genomes.ElementAt(0).Age < 10) // remove species with only one old genome

            // stay one genome species if historical fitness is close to the best
            .Where(x => x.Genomes.Count > 1 || x.Genomes.ElementAt(0).HistoricalFitness > worstFitness + ((bestFitness - worstFitness) * 0.5))
            .ToList();

        // edge case: when all species are empty after cleaning. E.g. in the beginning
        if (result.Count == 0)
        {
            var allGenomes = species
                .SelectMany(x => x.Genomes)
                .ToArray();

            result =
            [
                new Specie
                {
                    Genomes = allGenomes.Take((int) Math.Max(1, allGenomes.Length * killRate)).ToArray(),
                },
            ];
        }

        return result;
    }

    private record SpecieWithOffspring
    {
        public required Specie Specie { get; init; }
        public required int OffspringCount { get; set; }
    }
}
