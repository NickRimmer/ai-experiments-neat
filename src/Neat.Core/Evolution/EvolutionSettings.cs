using System.Diagnostics.CodeAnalysis;
namespace Neat.Core.Evolution;

public record EvolutionSettings
{
    public bool AllowRecurrent { get; init; }
    public WeightRange SynapseWeightRange { get; init; } = new (-4f, 4f);
    public int? MaximumHiddenNeurons { get; init; }

    // structural mutations probabilities
    public float StructAddSynapsesProbability { get; init; } = .2f; // synapse add
    public float StructAddDirectSynapsesProbability { get; init; } = 0f; // direct synapse between inputs and outputs add
    public float StructEnableSynapsesProbability { get; init; } = .3f; // synapse enable
    public float StructDisableSynapsesProbability { get; init; } = .3f; // synapse disable
    public float StructToggleSynapsesProbability { get; init; } = 0f; // synapse enable/disable
    public float StructNeuronAddProbability { get; init; } = .1f; // hidden neuron add
    public float StructNeuronRemoveProbability { get; init; } = .1f; // hidden neuron remove

    // non-structural mutations probabilities
    public float NonStructSynapseModifyProbability { get; init; } = .5f; // synapse weight change by SynapseWeightMutationPower
    public float NonStructSynapseReplaceProbability { get; init; } = .1f; // synapse weight change by random value
    public float NonStructNeuronActivationReplaceProbability { get; init; } = .1f; // activation function of random neuron change
    public float NonStructNeuronBiasProbability { get; set; } = .3f; // how often bias of neuron is changing

    public Dictionary<string, float> OverrideActivationProbabilities { get; init; } = new ();
}

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
public record WeightRange(float Min, float Max);
