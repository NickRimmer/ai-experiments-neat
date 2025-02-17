using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Trainer.Simulations.FieldRunner.Models;
using Neat.Trainer.Simulations.FieldRunner.Services;

namespace Neat.Viewer.Components.Controls;

public partial class FieldRunnerPlayer : ComponentBase
{
    private Genotype _genome = null!;
    private WorldData? _world;
    private WorldField[]? _timeline;

    public FieldRunnerPlayer()
    {
        var worldSize = new Size(50, 50);
        Settings = new WorldSettings
        {
            WorldSize = worldSize,
            InitialFoodCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
            ObstaclesCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
            PoisonsCount = (int) Math.Ceiling(worldSize.Width * worldSize.Height * .01),
        };
    }

    public WorldSettings Settings { get; set; }
    public int CurrentFrame { get; set; }
    public int Energy { get; set; }
    public bool IsPlaying { get; set; }
    public bool IsAutoReloadEnabled { get; set; }

    [Parameter]
    [EditorRequired]
    [SuppressMessage("Usage", "BL0007:Component parameters should be auto properties")]
    public Genotype Genome
    {
        get => _genome;
        set
        {
            if (_genome == value) return;
            _genome = value;

            ResetPlayer();
        }
    }

    private void OnPlaySimulation()
    {
        if (IsPlaying) return;

        IsPlaying = true;
        Task.Run(async () =>
        {
            while (IsPlaying && _timeline != null)
            {
                PlayFrame(+1, false);
                if (CurrentFrame >= _timeline.Length - 1)
                {
                    if (IsAutoReloadEnabled)
                        ResetPlayer();

                    CurrentFrame = 0; // loop
                }

                await InvokeAsync(StateHasChanged);
                await Task.Delay(100);
            }
        }).ConfigureAwait(true);
    }

    private void OnPauseSimulation()
    {
        IsPlaying = false;
    }

    private void ResetPlayer()
    {
        if (_genome == null) return;

        CurrentFrame = 0;
        var pika = new PhenotypeRunner(PhenotypeBuilder.Build(_genome));
        var simulation = new TheWorld([pika], Settings);
        simulation.Simulate(CancellationToken.None);
        _world = simulation.World;
        _timeline = _world.Timeline.Reverse().ToArray();
    }

    private void PlayFrame(int step, bool stopAutoPlay)
    {
        if (_timeline == null) return;
        CurrentFrame = Math.Min(_timeline.Length - 1, CurrentFrame + step);
        Energy = _timeline[CurrentFrame].Cells.Select(x => x?.Item).OfType<PikaWorldItem>().FirstOrDefault()?.Energy ?? 0;

        if (stopAutoPlay) IsPlaying = false;
    }
}
