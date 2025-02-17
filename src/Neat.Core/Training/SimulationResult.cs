using System.Diagnostics.CodeAnalysis;
using Neat.Core.Genomes;
namespace Neat.Core.Training;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
public record SimulationResult(Genotype Genome, float Fitness);
