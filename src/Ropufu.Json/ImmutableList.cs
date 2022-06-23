using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

using AllowSingelton = FalseType;

[JsonConverter(typeof(ImmutableListConverterFactory<AllowSingelton>))]
[NoexceptJsonConverter(typeof(ImmutableListNoexceptConverterFactory<AllowSingelton>))]
public sealed class ImmutableList<T>
    : IReadOnlyCollection<T>,
    IEnumerable<T>,
    IReadOnlyList<T>
{
    private readonly List<T> _values;

    private ImmutableList(T value, object? _)
    {
        _values = new(capacity: 1);
        _values.Add(value);
    }

    public ImmutableList()
        => _values = new(capacity: 0);

    public ImmutableList(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _values = new();
        _values.AddRange(collection);
        _values.TrimExcess();
    }

    public ImmutableList(List<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _values = new(capacity: collection.Count);
        _values.AddRange(collection);
    }

    public static ImmutableList<T> Singleton(T value)
        => new(value, null);

    /// <summary>
    /// Creates a mutable copy of this collection.
    /// </summary>
    public List<T> ToList()
    {
        List<T> result = new(capacity: _values.Count);
        result.AddRange(_values);
        return result;
    }

    /// <summary>
    /// Represents this collection as <see cref="ReadOnlyCollection"/>.
    /// </summary>
    public ReadOnlyCollection<T> ToReadOnly()
        => _values.AsReadOnly();

    public IEnumerator<T> GetEnumerator()
        => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    public int Count
        => _values.Count;

    public T this[int index]
        => _values[index];
}
