using Microsoft.AspNetCore.Components;
using Neat.Core.Genomes;
using Neat.Core.Species;
using Neat.Trainer.Modules.Config;
using Neat.Trainer.Modules.Storage;

namespace Neat.Viewer.Components.Pages;

public partial class FieldSim : ComponentBase
{
    private Genotype? _selectedGenome;

    public IReadOnlyCollection<Specie> Species { get; set; } = [];
    public int SelectedGenomeIndex { get; set; }
    public ConfigModel SelectedConfig { get; set; } = null!;
    public StorageGenData? SelectedGenData { get; set; }

    public Genotype? SelectedGenome
    {
        get => _selectedGenome;
        set { _selectedGenome = value; }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadSelectedConfig();
    }

    private void LoadSelectedConfig()
    {
        SelectedConfig = ConfigService.GetSettings("field-runner");
        if (SelectedConfig == null) return;

        using var service = new StorageService(SelectedConfig);
        SelectedGenData = service.ReadGenData();
        Species = SelectedGenData.Species.OrderByDescending(x => x.AverageFitness).ToList();

        ShowGenome(SelectedGenomeIndex);
    }

    private void ShowGenome(int index)
    {
        SelectedGenomeIndex = index;
        SelectedGenome = Species.SelectMany(x => x.Genomes).ElementAtOrDefault(index);
        StateHasChanged();
    }
}
