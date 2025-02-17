using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
namespace Neat.Core.Common;

public static class EnumerableExtensions
{
    public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (predicate(item)) return index;
            index++;
        }

        return -1;
    }

    public static bool TryFindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate, [NotNullWhen(true)] out int? index)
    {
        var i = 0;
        foreach (var item in source)
        {
            if (predicate(item))
            {
                index = i;
                return true;
            }

            i++;
        }

        index = null;
        return false;
    }

    public static List<TResult> ToList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source
        .Select(selector)
        .ToList();

    public static List<TResult> ToList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector) => source
        .Select(selector)
        .ToList();

    public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source
        .Select(selector)
        .ToArray();

    public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector) => source
        .Select(selector)
        .ToArray();

    public static IReadOnlyCollection<T> IfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> defaultValue)
    {
        var list = source.ToList();
        return list.Count > 0 ? list : defaultValue.ToList();
    }

    public static float? SumOrDefault(this IEnumerable<float?> source)
    {
        var list = source.Where(x => x != null).ToList();
        return list.Count == 0 ? null : list.Sum();
    }

    public static T Random<T>(this IEnumerable<T> source) => source.Shuffle().First();
    public static T? RandomOrDefault<T>(this IEnumerable<T> source) => source.Shuffle().FirstOrDefault();

    public static List<TResult> TrimToEnd<TResult>(this List<TResult> source, int maxLength) => source.Count > maxLength
        ? source.GetRange(source.Count - maxLength, maxLength)
        : source;

    public static ConcurrentBag<T> AddRange<T>(this ConcurrentBag<T> source, IEnumerable<T> items)
    {
        foreach (var item in items) source.Add(item);
        return source;
    }

    public static Queue<T> EnqueueRange<T>(this Queue<T> source, IEnumerable<T> items)
    {
        foreach (var item in items) source.Enqueue(item);
        return source;
    }

    public static IEnumerable<T> Times<T>(this int count, Func<int, T> builder) => Enumerable
        .Range(0, count)
        .Select(builder);

    public static IEnumerable<T> Times<T>(this int count, Func<T> builder)
    {
        if (count <= 0) return [];
        return Enumerable
            .Range(0, count)
            .Select(_ => builder());
    }
}
