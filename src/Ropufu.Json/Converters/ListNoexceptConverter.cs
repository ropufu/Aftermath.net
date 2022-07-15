using System.Text.Json;

namespace Ropufu.Json;

public sealed class ListNoexceptConverter<T>
    : NoexceptJsonConverter<List<T?>?>
{
    private class Medium
    {
        private readonly Utf8JsonParser<T?> _valueParser;
        private readonly bool _doAllowSingleton;

        public Medium(Utf8JsonParser<T?> valueParser, bool doAllowSingleton)
        {
            _valueParser = valueParser;
            _doAllowSingleton = doAllowSingleton;
        }

        public bool TryGetNotNull(ref Utf8JsonReader json, out List<T?>? value)
            => json.TokenType switch
            {
                JsonTokenType.StartArray => this.TryGetArray(ref json, out value),
                _ => this.TryGetSingleton(ref json, out value)
            };

        public bool TryGetMaybeNull(ref Utf8JsonReader json, out List<T?>? value)
        {
            switch (json.TokenType)
            {
                case JsonTokenType.Null:
                    value = null;
                    return true;
                case JsonTokenType.StartArray:
                    return this.TryGetArray(ref json, out value);
                default:
                    return this.TryGetSingleton(ref json, out value);
            } // switch (...)
        }

        private bool TryGetArray(ref Utf8JsonReader json, out List<T?>? value)
        {
            value = new();

            bool isGood = true;

            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
            {
                if (_valueParser(ref json, out T? x))
                    value.Add(x);
                else
                {
                    isGood = false;
                    value.Add(default);
                    json.Skip();
                } // else
            } // for (...)

            // Guard against malformed JSON.
            if (json.TokenType != JsonTokenType.EndArray)
                isGood = false;

            return isGood;
        }

        private bool TryGetSingleton(ref Utf8JsonReader json, out List<T?>? value)
        {
            if (_doAllowSingleton && _valueParser(ref json, out T? x))
            {
                value = new(capacity: 1) { x };
                return true;
            } // if (...)
            else
            {
                value = null;
                return false;
            } // else
        }
    }

    private readonly Utf8JsonParser<T?>? _valueParser;

    public bool DoAllowSingleton { get; set; }

    public ListNoexceptConverter(bool doAllowSingleton = false)
        => this.DoAllowSingleton = doAllowSingleton;

    public ListNoexceptConverter(Utf8JsonParser<T?> valueParser, bool doAllowSingleton = false)
    {
        ArgumentNullException.ThrowIfNull(valueParser);

        _valueParser = valueParser;
        this.DoAllowSingleton = doAllowSingleton;
    }

    /// <summary>
    /// Fetches the explicit value parser or retrieves the cached one if none was provided.
    /// </summary>
    private Utf8JsonParser<T?> GetValueParser(NullabilityAwareType<List<T?>?> typeToConvert)
    {
        if (_valueParser is not null)
            return _valueParser;

        NullabilityAwareType<T?> valueType = typeToConvert.GetGenericArguments()[0].Promote<T?>();

        if (!NoexceptJson.TryMakeParser(valueType, out Utf8JsonParser<T?>? valueParser))
            throw new NotSupportedException("Converter for T in List`[T] not found.");

        return valueParser;
    }

    public override Utf8JsonParser<List<T?>?> MakeParser(NullabilityAwareType<List<T?>?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        Medium medium = new(this.GetValueParser(typeToConvert), this.DoAllowSingleton);

        return typeToConvert.IsNotNull
            ? medium.TryGetNotNull
            : medium.TryGetMaybeNull;
    }
}
