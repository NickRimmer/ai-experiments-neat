using Microsoft.AspNetCore.Components;
using World.FieldRunner.Game.Models;

namespace World.FieldRunner.Components.Controls;

public partial class PlayerControl : ComponentBase, IDisposable
{
    [Parameter]
    public WorldModel? Simulation { get; set; }

    [Parameter]
    public EventCallback OnLoadBestSimulation { get; set; }

    public List<WorldField>? Timeline { get; set; }
    public bool IsPlaying { get; set; }
    public int CurrentFrame { get; set; }
    public int Energy { get; set; }
    public string AutoLoadStatus => _autoLoadBest ? "+" : "-";

    private bool _autoLoadBest;

    public void Dispose()
    {
        IsPlaying = false;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // reset frame position
        CurrentFrame = 0;
        Timeline = Simulation?.Timeline.Reverse().ToList();
    }

    private void OnAutoLoadSimulation()
    {
        _autoLoadBest = !_autoLoadBest;
    }

    private void OnPlaySimulation()
    {
        IsPlaying = false;
        PlaySimulation();
    }

    private void PlaySimulation()
    {
        IsPlaying = true;
        Task.Run(async () =>
        {
            while (IsPlaying && Timeline != null)
            {
                FrameForward();
                if (CurrentFrame >= Timeline.Count - 1)
                {
                    if (_autoLoadBest && OnLoadBestSimulation.HasDelegate)
                        await InvokeAsync(OnLoadBestSimulation.InvokeAsync);

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

    private void OnFrameBackward()
    {
        OnPauseSimulation();
        FrameBackward();
    }

    private void OnFrameForward()
    {
        OnPauseSimulation();
        FrameForward();
    }

    private void FrameBackward()
    {
        CurrentFrame = Math.Max(0, CurrentFrame - 1);
    }

    private void FrameForward()
    {
        if (Timeline == null) return;
        CurrentFrame = Math.Min(Timeline.Count - 1, CurrentFrame + 1);
        Energy = Timeline[CurrentFrame].Cells.Select(x => x?.Item).OfType<PikaWorldItem>().FirstOrDefault()?.Energy ?? 0;
    }
}
