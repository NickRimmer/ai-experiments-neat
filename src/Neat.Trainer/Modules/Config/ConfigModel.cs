using Neat.Core.Evolution;
using Neat.Core.Species;
using Neat.Core.Training;
namespace Neat.Trainer.Modules.Config;

public record ConfigModel
{
    public required SimulationSettings Simulation { get; init; }
    public EvolutionSettings Evolution { get; init; } = new ();
    public TrainingSettings Training { get; init; } = new ();
    public SpeciesSettings Species { get; init; } = new ();
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, float[]>? TestCases { get; init; }
}
