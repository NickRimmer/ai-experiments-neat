using System.Diagnostics.CodeAnalysis;
namespace Neat.Core.Genomes;

public class GenomesContext
{
    private readonly Dictionary<uint, Innovation> _innovations = new ();
    private bool _isInitialized;

    public uint GetInnovation(Guid inputNeuronId, Guid outputNeuronId)
    {
        if (!_isInitialized) throw new InvalidOperationException("Neat context is not initialized");

        var innovation = _innovations
            .Where(x => x.Value.InputNeuronId == inputNeuronId && x.Value.OutputNeuronId == outputNeuronId)
            .Select(x => (uint?) x.Key)
            .FirstOrDefault();

        if (!innovation.HasValue)
        {
            innovation = _innovations.Keys.Count == 0 ? 0 : _innovations.Keys.Max() + 1;
            _innovations[innovation.Value] = new Innovation(inputNeuronId, outputNeuronId);
        }

        return innovation.Value;
    }

    public void Rebuild(IEnumerable<Genotype> genomes)
    {
        _innovations.Clear();

        foreach (var genome in genomes)
        {
            foreach (var synapse in genome.Synapses)
            {
                if (_innovations.ContainsKey(synapse.Innovation)) continue;
                _innovations[synapse.Innovation] = new Innovation(synapse.InputNeuronId, synapse.OutputNeuronId);
            }
        }

        _isInitialized = true;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
    private record Innovation(Guid InputNeuronId, Guid OutputNeuronId);
}
