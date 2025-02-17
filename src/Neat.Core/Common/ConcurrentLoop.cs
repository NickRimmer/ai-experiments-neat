namespace Neat.Core.Common;

public class ConcurrentLoop<T>
{
    private readonly IReadOnlyCollection<T> _values;
    private readonly object _lock = new ();
    private int _cursor;

    public ConcurrentLoop(IEnumerable<T> source)
    {
        _values = source.ToList();
    }

    public ConcurrentLoop(IReadOnlyCollection<T> source)
    {
        _values = source;
    }

    public T GetNext()
    {
        lock (_lock)
        {
            if (_cursor >= _values.Count) _cursor = 0;
            return _values.ElementAt(_cursor++);
        }
    }

    public IEnumerable<T> GetNext(int count)
    {
        lock (_lock)
        {
            for (var i = 0; i < count; i++)
            {
                if (_cursor >= _values.Count) _cursor = 0;
                yield return _values.ElementAt(_cursor++);
            }
        }
    }
}
