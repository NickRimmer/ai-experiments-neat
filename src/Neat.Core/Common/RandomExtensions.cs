namespace Neat.Core.Common;

public static class RandomExtensions
{
    private static readonly Random Rnd = new ();

    public static bool NextBool(this Random random) =>
        random.Next(2) == 1;

    public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source) =>
        source.OrderBy(_ => Rnd.NextDouble());
}
