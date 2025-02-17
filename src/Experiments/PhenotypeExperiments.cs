// using Neat.Core.Modules.Neat;
// using Neat.Core.Modules.Neat.Models;
// namespace Experiments;
//
// public class PhenotypeExperiments
// {
//     [Test]
//     public void BuildPhenotype_Valid_ExpectedResults()
//     {
//         var genotype = new Genotype
//         {
//             Neurons =
//             [
//                 new Neuron { Type = NeuronType.Bias, Id = 0 },
//                 new Neuron { Type = NeuronType.Input, Id = 1 },
//                 new Neuron { Type = NeuronType.Input, Id = 2 },
//                 new Neuron { Type = NeuronType.Input, Id = 3 },
//
//                 // hidden
//                 new Neuron { Type = NeuronType.Hidden, Id = 4 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 5 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 6 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 7 },
//
//                 // output
//                 new Neuron { Type = NeuronType.Output, Id = 8 },
//                 new Neuron { Type = NeuronType.Output, Id = 9 },
//                 new Neuron { Type = NeuronType.Output, Id = 10 },
//             ],
//             Synapses =
//             [
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 4, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 8, Id = 0, IsEnabled = true, Weight = .75f },
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
//         PhenotypeBuilder.TryBuild(
//             genotype,
//             out var phenotype).Should().BeTrue();
//         phenotype!.ExecutionPlan.Length.Should().Be(7);
//
//         // expected dependencies
//         var expectedDependencies = new[]
//         {
//             (5, new uint[] { 5 }),
//             (6, [5, 1, 2]),
//             (8, [0]),
//             (10, [6]),
//         };
//
//         foreach (var expected in expectedDependencies)
//         {
//             var item = phenotype.ExecutionPlan.FirstOrDefault(x => x.TargetNeuronIndex == expected.Item1);
//             item.Should().NotBeNull("Neuron expected to be in execution plan");
//             (item!.Dependencies.Length == expected.Item2.Length).Should().BeTrue("Same number of dependencies expected");
//             (item.Dependencies.Select(x => genotype.Neurons[x.ActivationIndex].Id).Intersect(expected.Item2).Count() == expected.Item2.Length).Should().BeTrue("Same dependencies expected");
//         }
//     }
//
//     [Test]
//     public void BuildPhenotype_InvalidInput_Null()
//     {
//         var genome = new Genotype
//         {
//             Neurons =
//             [
//                 new Neuron { Type = NeuronType.Bias, Id = 0 },
//                 new Neuron { Type = NeuronType.Input, Id = 1 },
//                 new Neuron { Type = NeuronType.Input, Id = 2 },
//                 new Neuron { Type = NeuronType.Input, Id = 3 },
//
//                 // hidden
//                 new Neuron { Type = NeuronType.Hidden, Id = 4 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 5 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 6 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 7 },
//
//                 // output
//                 new Neuron { Type = NeuronType.Output, Id = 8 },
//                 new Neuron { Type = NeuronType.Output, Id = 9 },
//                 new Neuron { Type = NeuronType.Output, Id = 10 },
//             ],
//             Synapses =
//             [
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 4, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 8, Id = 0, IsEnabled = true, Weight = .75f },
//                 new Synapse { InputNeuronId = 1, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = -.2f },
//                 new Synapse { InputNeuronId = 2, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .22f },
//
//                 // hidden
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 5, Id = 0, IsEnabled = true, Weight = -.1f }, // self recurrent
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .1f },
//                 new Synapse { InputNeuronId = 6, OutputNeuronId = 10, Id = 0, IsEnabled = true, Weight = .31f },
//                 new Synapse { InputNeuronId = 7, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//
//                 // invalid synapse
//                 new Synapse { InputNeuronId = 2, OutputNeuronId = 1, Id = 99, IsEnabled = true, Weight = 0f }, // input pointed to input
//             ],
//         };
//
//         var result = PhenotypeBuilder.TryBuild(genome, out var phenotype);
//         result.Should().BeFalse();
//         phenotype.Should().BeNull();
//     }
//
//     [Test]
//     public void BuildPhenotype_InvalidOutput_Null()
//     {
//         var genome = new Genotype
//         {
//             Neurons =
//             [
//                 new Neuron { Type = NeuronType.Bias, Id = 0 },
//                 new Neuron { Type = NeuronType.Input, Id = 1 },
//                 new Neuron { Type = NeuronType.Input, Id = 2 },
//                 new Neuron { Type = NeuronType.Input, Id = 3 },
//
//                 // hidden
//                 new Neuron { Type = NeuronType.Hidden, Id = 4 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 5 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 6 },
//                 new Neuron { Type = NeuronType.Hidden, Id = 7 },
//
//                 // output
//                 new Neuron { Type = NeuronType.Output, Id = 8 },
//                 new Neuron { Type = NeuronType.Output, Id = 9 },
//                 new Neuron { Type = NeuronType.Output, Id = 10 },
//             ],
//             Synapses =
//             [
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 4, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//                 new Synapse { InputNeuronId = 0, OutputNeuronId = 8, Id = 0, IsEnabled = true, Weight = .75f },
//                 new Synapse { InputNeuronId = 1, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = -.2f },
//                 new Synapse { InputNeuronId = 2, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .22f },
//
//                 // hidden
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 5, Id = 0, IsEnabled = true, Weight = -.1f }, // self recurrent
//                 new Synapse { InputNeuronId = 5, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = .1f },
//                 new Synapse { InputNeuronId = 6, OutputNeuronId = 10, Id = 0, IsEnabled = true, Weight = .31f },
//                 new Synapse { InputNeuronId = 7, OutputNeuronId = 6, Id = 0, IsEnabled = true, Weight = 0f }, // dead neuron
//
//                 // invalid synapse
//                 new Synapse { InputNeuronId = 8, OutputNeuronId = 10, Id = 99, IsEnabled = true, Weight = 0f }, // output pointed to output
//             ],
//         };
//
//         var result = PhenotypeBuilder.TryBuild(genome, out var phenotype);
//         result.Should().BeFalse();
//         phenotype.Should().BeNull();
//     }
// }
