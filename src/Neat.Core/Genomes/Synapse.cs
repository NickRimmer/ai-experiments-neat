namespace Neat.Core.Genomes;

public record Synapse
{
    public required uint Innovation { get; init; }
    public required Guid InputNeuronId { get; init; }
    public required Guid OutputNeuronId { get; init; }
    public required float Weight { get; init; }
    public required bool IsEnabled { get; init; }
}
