using System.Diagnostics.CodeAnalysis;
using Neat.Core.Genomes;
namespace Neat.Core.Phenotypes;

[SuppressMessage("ReSharper", "TypeWithSuspiciousEqualityIsUsedInRecord.Global")]
public record Phenotype
{
    public required Genotype Genome { get; init; }
    public required ExecutionItem[] ExecutionPlan { get; init; }
    public required float[] Activations { get; set; }
    public required float?[] Memory { get; init; } // previous activations for self-recurrent neurons
}

public record ExecutionItem
{
    public required int TargetNeuronIndex { get; init; }
    public required ExecutionDependency[] Dependencies { get; init; }
    public required Func<float, float, float> ActivationFunction { get; init; }
    public bool IsRecurrent { get; init; } // self-recurrent or loop-recurrent, when true then activation storing in Memory for the next iteration
}

public struct ExecutionDependency
{
    public required int ActivationIndex { get; init; }
    public required float Weight { get; init; }
}
