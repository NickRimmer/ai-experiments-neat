using System.Text.Json.Serialization;
namespace Neat.Core.Species;

public record SpeciesSettings
{
    /// <summary>
    /// Can be used to adjust threshold. Must be less than count of genomes.
    /// </summary>
    public int SpeciesTargetCount { get; init; } = 8; // good to have 5-15
    public SpecieDistanceFunction DistanceFunction { get; init; } = SpecieDistanceFunction.MinDistanceToAllGenomes;

    /// <summary>
    /// How much will be added or subtracted from threshold to adjust it, to have 'SpeciesTargetCount' species count.
    /// </summary>
    public float DistanceThresholdAdjustmentRate { get; init; } = .05f;

    public float ExcessCoefficient { get; init; } = 1f;
    public float DisjointCoefficient { get; init; } = 1f;
    public float WeightCoefficient { get; init; } = .4f;
    public float ActivationDiffCoefficient { get; init; } = 1f;
    public float BiasDiffCoefficient { get; init; } = .4f;

    public float NormalizationFactor { get; init; } = 1f;

    /// <summary>
    /// Genome distance threshold for species grouping.
    /// </summary>
    [JsonIgnore] // value will be set automatically and adjusted during execution.
    public float? SpeciesThreshold { get; set; }

    public enum SpecieDistanceFunction
    {
        MinDistanceToAllGenomes,
        DistanceToRandomGenome,
        DistanceToHalfRandomGenomes,
    }
}
