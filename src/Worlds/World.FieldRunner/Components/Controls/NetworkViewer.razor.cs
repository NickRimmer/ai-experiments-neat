using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;
using World.FieldRunner.Game.Models;

namespace World.FieldRunner.Components.Controls;

public partial class NetworkViewer : ComponentBase
{
    [Inject]
    public IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public WorldModel? Simulation { get; set; }
    private WorldModel? _currentSimulation;

    public string NetworkMode { get; private set; } = "Unset";

    private IJSObjectReference _jsModule = null!;
    private int _networkMode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Controls/NetworkViewer.razor.js");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (_currentSimulation?.Id != Simulation?.Id)
        {
            _currentSimulation = Simulation;
            await DrawNetwork();
        }
    }

    public Task SwitchNetwork()
    {
        _networkMode = (_networkMode + 1) % 3;
        return DrawNetwork();
    }

    public async Task DrawNetwork()
    {
        var pika = _currentSimulation?
            .Timeline
            .Last() // as it is stack and order is reversed
            .Cells
            .Select(x => x?.Item)
            .OfType<PikaWorldItem>()
            .FirstOrDefault();

        if (pika == null) return;

        IEnumerable<Neuron> neurons = pika.PhenotypeRunner.Phenotype.Genome.Neurons;
        IEnumerable<Synapse> synapses = pika.PhenotypeRunner.Phenotype.Genome.Synapses;

        switch (_networkMode)
        {
            // active and bias, input, output
            case 0:
                NetworkMode = "Active and Bias, Input, Output";
                (neurons, synapses) = PhenotypeBuilder.CleanDeadNeurons(pika.PhenotypeRunner.Phenotype.Genome.Neurons, pika.PhenotypeRunner.Phenotype.Genome.Synapses);

                // always include bias, input and output neurons
                neurons = [..neurons, ..pika.PhenotypeRunner.Phenotype.Genome.Neurons.Where(x => x.Type is NeuronType.Bias or NeuronType.Input or NeuronType.Output)];
                neurons = neurons.DistinctBy(x => x.Id).ToList();
                break;

            // only active
            case 1:
                NetworkMode = "Only Active";
                (neurons, synapses) = PhenotypeBuilder.CleanDeadNeurons(pika.PhenotypeRunner.Phenotype.Genome.Neurons, pika.PhenotypeRunner.Phenotype.Genome.Synapses);
                break;

            // all with disabled synapses
            case 2:
                NetworkMode = "All from Genome";
                break;
        }

        await _jsModule.InvokeVoidAsync("drawGraph", JsonSerializer.Serialize(new
        {
            nodes = neurons.Select(x => new
            {
                id = x.Id,
                group = x.Type switch
                {
                    NeuronType.Bias => 0,
                    NeuronType.Input => 0,
                    NeuronType.Hidden => 1,
                    NeuronType.Output => 2,
                    _ => throw new ArgumentOutOfRangeException(),
                },
                label = x.Label ?? string.Empty,
                level = x.Type switch
                {
                    NeuronType.Bias => 0,
                    NeuronType.Input => 0,
                    NeuronType.Hidden => 1,
                    NeuronType.Output => 2,
                    _ => throw new ArgumentOutOfRangeException(),
                },
            }),

            links = synapses.Select(x => new
            {
                source = x.InputNeuronId,
                target = x.OutputNeuronId,
                strength = x.Weight,
                disabled = !x.IsEnabled,
            }),
        }));
        StateHasChanged();
    }
}
