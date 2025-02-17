using System.Globalization;
using Microsoft.AspNetCore.Components;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using Neat.Core.Species;
using Neat.Core.Training;
using Neat.Trainer.Modules.Config;
using Neat.Trainer.Modules.Storage;
using Neat.Viewer.Modules.Storage;

namespace Neat.Viewer.Components.Pages;

public partial class Home : ComponentBase
{
    private string? _selectedConfigName;
    private Genotype? _selectedGenome;

    public IReadOnlyCollection<Specie> Species { get; set; } = [];
    public List<ConfigModel> Configs { get; set; } = [];
    public int SelectedGenomeIndex { get; set; }
    public ConfigModel? SelectedConfig { get; set; }
    public StorageGenData? SelectedGenData { get; set; }

    public Genotype? SelectedGenome
    {
        get => _selectedGenome;
        set
        {
            _selectedGenome = value;

            if (!string.IsNullOrEmpty(TestResult))
            {
                TestResult = string.Empty;
                TestMe();
            }
            else
            {
                TestError = string.Empty;
            }
        }
    }

    public string? SelectedConfigName
    {
        get => _selectedConfigName;
        set
        {
            _selectedConfigName = value;
            LoadSelectedConfig();
        }
    }

    public string TestData { get; set; } = string.Empty;
    public string[] TestDataValues { get; set; } = new string[10];
    public string TestResult { get; set; } = string.Empty;
    public string TestError { get; set; } = string.Empty;

    public ElementReference? TestInput { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ReloadConfigs();
        LoadSelectedConfig();
    }

    public void ReloadConfigs()
    {
        Configs = ConfigsService.GetConfigs().ToList();
        if (SelectedConfigName == null) SelectedConfigName = Configs.FirstOrDefault()?.Name;

        StateHasChanged();
    }

    public void LoadSelectedConfig(bool resetSelection = true)
    {
        if (SelectedConfigName == null) return;
        SelectedConfig = Configs.FirstOrDefault(x => x.Name.Equals(_selectedConfigName));
        if (SelectedConfig == null) return;

        using var service = new StorageService(SelectedConfig);
        SelectedGenData = service.ReadGenData();
        Species = SelectedGenData.Species.OrderByDescending(x => x.AverageFitness).ToList();

        ShowGenome(resetSelection ? 0 : SelectedGenomeIndex);
    }

    private void ShowGenome(int index)
    {
        SelectedGenomeIndex = index;
        SelectedGenome = Species.SelectMany(x => x.Genomes).ElementAtOrDefault(index);
        StateHasChanged();
    }

    private void ResetTest()
    {
        TestResult = string.Empty;
        TestError = string.Empty;
        TestData = string.Empty;
        TestDataValues = new string[10];
        StateHasChanged();
    }

    private void TestMe()
    {
        TestResult = string.Empty;
        TestError = string.Empty;

        var testData = TestData;
        if (SelectedGenome?.Neurons.Count(x => x.Type == NeuronType.Input) is <= 10)
            testData = string.Join(";", TestDataValues.Take(SelectedGenome.Neurons.Count(x => x.Type == NeuronType.Input)));

        if (string.IsNullOrEmpty(testData) || SelectedGenome == null)
        {
            TestError = "No input data or genome selected";
            return;
        }

        try
        {
            var inputs = testData.Split(';').Select(x => x.Trim()).Select(x => string.IsNullOrWhiteSpace(x) ? 0 : float.Parse(x)).ToArray();
            var result = PhenotypeBuilder.TryBuild(SelectedGenome, out var phenotype)
                ? new PhenotypeRunner(phenotype).Run(inputs)
                : [];

            var output = result.Where(x => x.Key.Type == NeuronType.Output).ToList();
            var values = output.Select(x => new
            {
                Fitness = x.Value,
                Neuron = x.Key,
            });

            TestResult = string.Join("\n", values
                .OrderByDescending(x => x.Fitness)
                .Select(x => new
                {
                    Title = x.Neuron.Data,
                    Value = Math.Round(x.Fitness, 4),
                })
                .Select(x => string.IsNullOrWhiteSpace(x.Title) ? x.Value.ToString(CultureInfo.InvariantCulture) : $"{x.Title}: {x.Value}"));

            if (string.IsNullOrWhiteSpace(TestResult)) TestResult = "No output";
        }
        catch (Exception ex)
        {
            TestError = ex.Message;
        }
        finally
        {
            TestInput?.FocusAsync();
        }
    }

    private void Simulate()
    {
        var simulationName = SelectedConfig?.Simulation?.Name;
        if (string.IsNullOrWhiteSpace(simulationName))
        {
            TestError = "No simulation available";
            return;
        }

        var simulationType = typeof(StorageService)
            .Assembly
            .GetTypes()
            .FirstOrDefault(x => x.GetInterfaces().Contains(typeof(ISimulation)) && x.Name.Equals(simulationName));

        if (simulationType == null)
        {
            TestError = "Simulation not found";
            return;
        }

        try
        {
            var simulation = Activator.CreateInstance(simulationType) as ISimulation;
            if (simulation == null)
            {
                TestError = "Failed to create simulation";
                return;
            }

            if (SelectedGenome == null)
            {
                TestError = "No genome selected";
                return;
            }

            simulation.Initialize(new ConcurrentLoop<Genotype>([SelectedGenome]));
            var results = simulation.Run(CancellationToken.None);

            TestResult = string.Join("\n", results.Select(x => x.Fitness.ToString(CultureInfo.InvariantCulture)));
        }
        catch (Exception ex)
        {
            TestError = $"Failed to run simulation: {ex.Message}";
        }
    }

    private void RunTestCase(float[] values)
    {
        TestData = string.Empty;
        TestDataValues = values.Select(x => x.ToString(CultureInfo.InvariantCulture) ?? string.Empty).ToArray();
        TestMe();
    }
}
