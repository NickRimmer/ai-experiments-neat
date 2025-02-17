using Neat.Core.Species;
namespace Neat.Trainer.Modules.Storage;

public record StorageGenData
{
    public required IReadOnlyCollection<Specie> Species { get; init; }
    public float? SpeciesThreshold { get; init; }

    public required int Iteration { get; init; }
}
