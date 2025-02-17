// using System.Diagnostics.CodeAnalysis;
// using Neat.Core.Modules.Neat;
// using Neat.Core.Modules.Neat.Models;
// namespace Experiments;
//
// public class RunnerExperiments
// {
//     [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1123:Do not place regions within elements")]
//     [Test]
//     public void Run()
//     {
//
//         #region genotype
//
//         Neuron[] neurons =
//         [
//             new () { Type = NeuronType.Bias, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Input, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Input, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Input, Id = Guid.NewGuid() },
//
//             // hidden
//             new () { Type = NeuronType.Hidden, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Hidden, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Hidden, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Hidden, Id = Guid.NewGuid() },
//
//             // output
//             new () { Type = NeuronType.Output, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Output, Id = Guid.NewGuid() },
//             new () { Type = NeuronType.Output, Id = Guid.NewGuid() },
//         ];
//
//         var genotype = new Genotype
//         {
//             Neurons = neurons,
//             Synapses =
//             [
//                 new Synapse { InputNeuronId = neurons[0].Id, OutputNeuronId = neurons[4].Id, Id = Guid.NewGuid(), IsEnabled = true, Weight = 0f }, // dead neuron
//                 new Synapse { InputNeuronId = neurons[0].Id, OutputNeuronId = 8, Id = 0, IsEnabled = true, Weight = .75f },
//                 new Synapse { InputNeuronId = 1, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = -.2f },
//                 new Synapse { InputNeuronId = 2, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .22f },
//
//                 // hidden
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 5, Id = 0, IsEnabled = true, Weight = -.1f }, // self recurrent
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .1f },
//                 new Synapse { InputNeuronId = 6, OutputNeuronId = 10, Id = 0, IsEnabled = true, Weight = .31f },
//                 new Synapse { InputNeuronId = 7, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//             ],
//         };
//
//         #endregion
//
//         if (!PhenotypeBuilder.TryBuild(genotype, out var phenotype))
//             Assert.Fail("Phenotype build failed");
//
//         var runner = new PhenotypeRunner(phenotype!);
//
//         // act
//         var result = runner.Run([.5f, 0f, 0f]);
//
//         // assert
//         var expected = new[] { 0.635f, -0.0308f };
//         var values = result.Values.ToArray();
//         for (var i = 0; i < expected.Length; i++)
//             values[i].Should().BeApproximately(expected[i], 0.001f);
//     }
// }
