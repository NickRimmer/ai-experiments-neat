using System.Text.Json.Serialization;
namespace Neat.Core.Genomes;

public record Genotype
{
    public required Neuron[] Neurons { get; init; }
    public required Synapse[] Synapses { get; init; }
    public float HistoricalFitness { get; init; }
    public uint Generation { get; init; }
    public uint Age { get; init; }

    [JsonIgnore] // used to make sure that genomes object is unique, as structures on comparison testing by properties, not reference.
    public Guid Id { get; } = Guid.NewGuid();
}
