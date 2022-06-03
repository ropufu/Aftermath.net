using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

[JsonConverter(typeof(ImmutableDictionaryConverterFactory))]
[NoexceptJsonConverter(typeof(ImmutableDictionaryNoexceptConverterFactory))]
public sealed class ImmutableDictionary<TKey, TValue>
    : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map;

    public ImmutableDictionary()
        => _map = new(capacity: 0);

    public ImmutableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _map = new();
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            _map.Add(pair.Key, pair.Value);
    }

    public ImmutableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _map = new(capacity: dictionary.Count);
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            _map.Add(pair.Key, pair.Value);
    }

    /// <summary>
    /// Creates a mutable copy of this collection.
    /// </summary>
    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> result = new(capacity: _map.Count);
        foreach (KeyValuePair<TKey, TValue> pair in _map)
            result.Add(pair.Key, pair.Value);
        return result;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => _map.Count;

    public TValue this[TKey key] => _map[key];

    public IEnumerable<TKey> Keys => _map.Keys;

    public IEnumerable<TValue> Values => _map.Values;

    public bool ContainsKey(TKey key) => _map.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _map.TryGetValue(key, out value);
}
