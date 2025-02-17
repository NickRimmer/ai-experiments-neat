using World.FieldRunner.Game.Models;
using World.FieldRunner.Game.Services;
namespace Experiments.FieldRunner;

public class SimulationExperiments
{
    [Test]
    public void Create()
    {
        var settings = new SimulationSettings
        {
            WorldSize = new (10, 10),
            InitialFoodCount = 3,
        };

        var simulation = new Simulation(settings, []);
        simulation.Should().NotBeNull();
    }

    [Test]
    public void Start()
    {
        var settings = new SimulationSettings
        {
            WorldSize = new (10, 10),
            InitialFoodCount = 3,
        };

        var simulation = new Simulation(settings, []);
        var result = simulation.Start(CancellationToken.None);

        result.Should().NotBeNull();
        result!.World.Timeline.Should().NotBeEmpty();
    }
}
