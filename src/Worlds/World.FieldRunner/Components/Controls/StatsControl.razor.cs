using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Neat.Core.Common;
using World.FieldRunner.Game.Services;

namespace World.FieldRunner.Components.Controls;

public partial class StatsControl : ComponentBase, IDisposable
{
    private LineChart _chart = null!;
    private LineChartOptions _options = null!;
    private ChartData _data = null!;
    private LineChartDataset _dataset1 = null!;
    private LineChartDataset _dataset2 = null!;
    private int _lastSimulationCount = 0;
    private Timer? _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    protected override void OnInitialized()
    {
        var colors = ColorUtility.CategoricalTwelveColors;

        var labels = new List<string>();
        var datasets = new List<IChartDataset>(2);

        _dataset1 = new LineChartDataset
        {
            Label = "Best",
            Data = [],
            BackgroundColor = colors[0],
            BorderColor = colors[0],
            BorderWidth = 2,
            HoverBorderWidth = 4,

            // PointBackgroundColor = colors[0],
            // PointRadius = 0, // hide points
            // PointHoverRadius = 4,
        };
        datasets.Add(_dataset1);

        _dataset2 = new LineChartDataset
        {
            Label = "Worst",
            Data = [],
            BackgroundColor = colors[1],
            BorderColor = colors[1],
            BorderWidth = 2,
            HoverBorderWidth = 4,

            // PointBackgroundColor = colors[1],
            // PointRadius = 0, // hide points
            // PointHoverRadius = 4,
        };
        datasets.Add(_dataset2);

        _data = new ChartData { Labels = labels, Datasets = datasets };

        _options = new ()
        {
            Responsive = true,
            MaintainAspectRatio = false,
            Interaction = new Interaction { Mode = InteractionMode.Index },
        };

        _options.Scales.X!.Title = new ChartAxesTitle { Text = "Evaluations", Display = false };
        _options.Scales.Y!.Title = new ChartAxesTitle { Text = "Fitness", Display = true };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _chart.InitializeAsync(_data, _options);
            _timer = new Timer(_ => UpdateChartAsync().ConfigureAwait(false), null, 0, 30_000);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task UpdateChartAsync()
    {
        if (_lastSimulationCount == TrainingService.Instance.SimulationsCount) return;

        var results = TrainingService.Instance.History.Skip(_lastSimulationCount).ToList();
        if (results == null) return;

        _lastSimulationCount = TrainingService.Instance.SimulationsCount;
        var label = TrainingService.Instance.SimulationsCount.ToString();

        const int maxLength = 100;

        _data.Labels?.Add(label);
        _dataset1.Data!.Add(results.Max(x => x.Best));
        _dataset2.Data!.Add(results.Min(x => x.Worst));

        if (_dataset1.Data!.Count > maxLength * 1.25)
        {
            _data.Labels = _data.Labels?.TrimToEnd(maxLength);
            _dataset1.Data = _dataset1.Data.TrimToEnd(maxLength);
            _dataset2.Data = _dataset2.Data.TrimToEnd(maxLength);
        }

        ((LineChartDataset) _data.Datasets![0]).Data = _dataset1.Data;
        ((LineChartDataset) _data.Datasets![1]).Data = _dataset2.Data;
        await _chart.UpdateValuesAsync(_data);
    }
}
