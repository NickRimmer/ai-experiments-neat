using Microsoft.AspNetCore.Components;
using World.FieldRunner.Game.Models;
using World.FieldRunner.Game.Services;

namespace World.FieldRunner.Components.Pages;

public partial class Home : ComponentBase
{
    private WorldModel? _simulation;

    private void LoadLastSimulation()
    {
        _simulation = TrainingService.Instance.LastSimulation?.World;
    }

    private void LoadBestSimulation()
    {
        _simulation = TrainingService.Instance.BestSimulation?.World;
    }
}
