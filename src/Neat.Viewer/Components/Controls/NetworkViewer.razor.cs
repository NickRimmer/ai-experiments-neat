using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neat.Core.Genomes;
using Neat.Core.Phenotypes;

namespace Neat.Viewer.Components.Controls;

public partial class NetworkViewer : ComponentBase
{
    private Genotype _genome = null!;

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
            DrawNetworkAsync().ConfigureAwait(false);
        }
    }

    [Inject]
    public IJSRuntime JS { get; set; } = null!;

    public string NetworkMode { get; private set; } = "Unset";
    public int _forcesConfig;

    private IJSObjectReference _jsModule = null!;
    private int _networkMode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Controls/NetworkViewer.razor.js");
            await DrawNetworkAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    // protected override Task OnParametersSetAsync() => DrawNetwork();

    public Task SwitchNetwork()
    {
        _networkMode = (_networkMode + 1) % 3;
        return DrawNetworkAsync();
    }

    public Task SwitchForces()
    {
        _forcesConfig = (_forcesConfig + 1) % 2;
        return DrawNetworkAsync();
    }

    private async Task DrawNetworkAsync()
    {
        if (_jsModule == null) return;

        IEnumerable<Neuron> neurons = Genome.Neurons;
        IEnumerable<Synapse> synapses = Genome.Synapses;

        switch (_networkMode)
        {
            // active and bias, input, output
            case 0:
                NetworkMode = "Active and Bias, Input, Output";
                (neurons, synapses) = PhenotypeBuilder.CleanDeadNeurons(Genome.Neurons, Genome.Synapses);

                // always include bias, input and output neurons
                neurons = [..neurons, ..Genome.Neurons.Where(x => x.Type is NeuronType.Bias or NeuronType.Input or NeuronType.Output)];
                neurons = neurons.DistinctBy(x => x.Id).ToList();
                break;

            // only active
            case 1:
                NetworkMode = "Only Active";
                (neurons, synapses) = PhenotypeBuilder.CleanDeadNeurons(Genome.Neurons, Genome.Synapses);
                break;

            // all with disabled synapses
            case 2:
                NetworkMode = "All from Genome";
                break;
        }

        var json = JsonSerializer.Serialize(new
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
                label = x.Type != NeuronType.Hidden
                    ? (x.Label ?? string.Empty)
                    : $"{x.ActivationFunction.GetAbbreviation()}{Math.Round(x.Bias, 1)}", // abbreviate activation function
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
        });

        await _jsModule.InvokeVoidAsync(
            "drawGraph",
            json,
            _forcesConfig);
        StateHasChanged();
    }
}
