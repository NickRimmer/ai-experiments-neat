namespace Neat.Core.Training;

public record TrainingSettings
{
    public int SimulationsAtOnce { get; init; } = 100;
    public float KillRate { get; init; } = 0.5f; // half of the best genomes will survive  by default
}
