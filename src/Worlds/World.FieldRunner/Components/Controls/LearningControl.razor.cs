using System.Drawing;
using Microsoft.AspNetCore.Components;
using World.FieldRunner.Game.Models;
using World.FieldRunner.Game.Services;

namespace World.FieldRunner.Components.Controls;

public partial class LearningControl : ComponentBase, IDisposable
{
    [Parameter]
    public EventCallback OnLoadLastSimulation { get; set; }

    [Parameter]
    public EventCallback OnLoadBestSimulation { get; set; }

    private Timer? _timer;

    private void LoadLastSimulation()
    {
        if (OnLoadLastSimulation.HasDelegate)
            OnLoadLastSimulation.InvokeAsync();
    }

    private void LoadBestSimulation()
    {
        if (OnLoadBestSimulation.HasDelegate)
            OnLoadBestSimulation.InvokeAsync();
    }

    public uint GenMin => TrainingService.Instance.Genomes.Count > 0 ? TrainingService.Instance.Genomes.Select(x => x.Generation).Min() : 0;
    public uint GenMax => TrainingService.Instance.Genomes.Count > 0 ? TrainingService.Instance.Genomes.Select(x => x.Generation).Max() : 0;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            _timer = new Timer(UpdateStats, null, TimeSpan.FromSeconds(.5), TimeSpan.FromSeconds(1));
        }
    }

    private void UpdateStats(object? state)
    {
        InvokeAsync(StateHasChanged);
    }

    private static void Start()
    {
        Setup();
        TrainingService.Instance.StartAsync().ConfigureAwait(false);
    }

    private static void Stop()
    {
        TrainingService.Instance.Stop();
    }

    private static Task ReplayAsync()
    {
        Setup();
        return TrainingService.Instance.ReplayAsync();
    }

    private static void Setup()
    {
        if (TrainingService.Instance.Settings is null)
        {
            var worldSize = new Size(50, 50);
            var settings = new SimulationSettings
            {
                WorldSize = worldSize,
                InitialFoodCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
                ObstaclesCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
                PoisonsCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
            };

            TrainingService.Setup(settings);
        }
    }
}
