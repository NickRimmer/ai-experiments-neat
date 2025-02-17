using System.Diagnostics.CodeAnalysis;
using System.Reflection;
namespace Neat.Core.Genomes;

// https://ml-explained.com/blog/activation-functions-explained
// https://www.desmos.com/calculator/z8lbhksaaa

public static class ActivationFunctions
{
    private static Dictionary<string, FunctionData>? _functionsInternal;
    private static Dictionary<string, FunctionData> Functions => _functionsInternal ??= BuildFunctionsList();

    public static IReadOnlyCollection<string> GetFunctions() => Functions.Keys;

    public static Func<float, float, float> GetFunction(string name)
    {
        if (!Functions.TryGetValue(name, out var function))
            throw new ArgumentException($"Unknown activation function {name}");

        return function.Function;
    }

    public static string GetRandomFunction(Dictionary<string, float> overrideProbabilities)
    {
        var functions = Functions
            .Select(x => new
            {
                Name = x.Key,
                Probability = overrideProbabilities.TryGetValue(x.Key, out var value) ? value : x.Value.Probability,
            })
            .Where(x => x.Probability > 0)
            .ToList();

        var totalProbability = functions.Sum(x => x.Probability);
        var randomValue = Random.Shared.NextDouble() * totalProbability;

        foreach (var fn in functions)
        {
            randomValue -= fn.Probability;
            if (randomValue <= 0)
                return fn.Name;
        }

        throw new InvalidOperationException("Failed to select random activation function");
    }

    /// <summary>
    /// Balanced (-1 to 1), smooth transitions, common for hidden layers.
    /// </summary>
    [Activation(.4)]
    public static float HyperbolicTangent(float x, float bias)
        => (float) Math.Tanh(x + bias);

    /// <summary>
    /// S-shaped (0 to 1), useful for probability-like behavior.
    /// </summary>
    [Activation(.3)]
    public static float Sigmoid(float x, float bias)
        => (float) (1 / (1 + Math.Exp(-x + bias)));

    /// <summary>
    /// Linear, used in output layers (or rare cases in hidden layers).
    /// </summary>
    [Activation(.05)]
    public static float Identity(float x, float bias)
        => x + bias;

    /// <summary>
    /// Strict ON/OFF switch, non-differentiable, useful for hard decisions.
    /// </summary>
    [Activation(.05)]
    public static float BinaryStep(float x, float bias)
        => x + bias < 0 ? 0 : 1;

    /// <summary>
    /// Peaks at 0, useful for localized activations, niche case.
    /// </summary>
    [Activation(.1)]
    public static float Gaussian(float x, float bias)
        => (float) Math.Exp(-Math.Pow(x - bias, 2));

    /// <summary>
    /// Advanced activation, better than ReLU, smooth, improves learning.
    /// Range -0.31..inf
    /// </summary>
    [Activation(0)]
    public static float Mish(float x, float bias)
    {
        var value = x + bias;
        return (float) (value * Math.Tanh(Math.Log(1 + Math.Exp(value))));
    }

    /// <summary>
    /// Very similar to Mish but can behave slightly differently in some networks.
    /// While Mish is always negative for negative inputs, Swish can be slightly positive for small negative inputs, which might allow different learning behaviors.
    /// </summary>
    [Activation(.1)]
    public static float Swish(float x, float bias)
    {
        var value = x + bias;
        return value * (float) (1 / (1 + Math.Exp(-value)));
    }

    /// <summary>
    /// Probabilities distribution function.
    /// https://www.desmos.com/calculator/ah8lzeusca
    /// </summary>
    public static float[] SoftMax(IReadOnlyCollection<float> inputs)
    {
        if (inputs.Count == 0) return [];

        var max = inputs.Max();
        var exps = inputs.Select(x => Math.Exp(x - max)).ToArray();
        var sum = exps.Sum();
        var result = exps.Select(x => (float) (x / sum)).ToArray();
        return result;
    }

    private static Dictionary<string, FunctionData> BuildFunctionsList()
    {
        // build list of functions with reflection
        var methods = typeof(ActivationFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.GetCustomAttribute<ActivationAttribute>() is not null)
            .ToDictionary(
                x => x.Name,
                x => new FunctionData(
                    x.CreateDelegate<Func<float, float, float>>(),
                    x.GetCustomAttribute<ActivationAttribute>()!.Probability));

        return methods;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter")]
    private record FunctionData(Func<float, float, float> Function, double Probability);

    private class ActivationAttribute : Attribute
    {
        public double Probability { get; }
        public ActivationAttribute(double probability)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(probability);
            Probability = probability;
        }
    }
}
