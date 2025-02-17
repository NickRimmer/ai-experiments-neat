using System.Text.Json;
using System.Text.Json.Serialization;
using Neat.Core.Genomes;
using World.FieldRunner.Game.Enums;
namespace World.FieldRunner.Game.Services;

public static class GenomesPool
{
    private static readonly JsonSerializerOptions JsonSettings = new ()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyCollection<Genotype> Create(string name, int count)
    {
        var emptyGenome = new Genotype
        {
            Generation = 0,
            Synapses = [],
            Neurons =
            [
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Bias, Label = "Bias" }, // bias

                // food
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food L" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food F" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Food R" },

                // poison
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois L" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois F" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Pois R" },

                // walls
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall L" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall F" },
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Wall R" },

                // hidden neurons
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, Label = "H1" },

                // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, Label = "H2" },
                // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, Label = "H3" },
                // new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Hidden, Label = "H4" },

                // additional signals
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "RND" }, // random -1 .. 1
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Input, Label = "Sin" }, // time based sin of angle

                // actions
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Left.ToString(), Label = "Move L" }, // move left
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Forward.ToString(), Label = "Move F" }, // move forward
                new Neuron { Id = Guid.NewGuid(), Type = NeuronType.Output, Data = Move.Right.ToString(), Label = "Move R" }, // move right
            ],
        };

        var genes = Enumerable
            .Range(0, count)
            .Select(_ => emptyGenome)
            .ToList();

        Save(name, genes);
        return genes;
    }

    public static IReadOnlyCollection<Genotype> Read(string name)
    {
        var path = GetStoragePath(name);
        if (!File.Exists(path)) return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<IReadOnlyCollection<Genotype>>(json, JsonSettings) ?? [];
    }

    public static void Save(string name, IReadOnlyCollection<Genotype> genomes)
    {
        var path = GetStoragePath(name);
        var json = JsonSerializer.Serialize(genomes, JsonSettings);
        File.WriteAllText(path, json);
    }

    private static string GetStoragePath(string name)
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "genomes");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        return Path.Combine(directory, $"{name}.json");
    }
}
