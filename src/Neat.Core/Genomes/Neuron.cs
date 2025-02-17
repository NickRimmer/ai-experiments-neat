namespace Neat.Core.Genomes;

public record Neuron
{
    public required Guid Id { get; init; }
    public required NeuronType Type { get; init; }
    public float Bias { get; init; }
    public string? Data { get; init; }
    public string? Label { get; init; }
    public string ActivationFunction { get; init; } = nameof(ActivationFunctions.Identity);
}

public enum NeuronType
{
    Input,
    Output,
    Hidden,
    Bias,
}
