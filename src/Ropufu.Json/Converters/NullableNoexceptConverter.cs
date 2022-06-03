using System.Text.Json;

namespace Ropufu.Json;

public class NullableNoexceptConverter<T>
    : NoexceptJsonConverter<T?>
    where T : struct
{
    private class Medium
    {
        private readonly Utf8JsonParser<T> _valueParser;

        public Medium(Utf8JsonParser<T> valueParser)
            => _valueParser = valueParser;

        public bool TryGet(ref Utf8JsonReader json, out T? value)
        {
            if (json.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            } // if (...)

            if (_valueParser(ref json, out T notNullValue))
            {
                value = notNullValue;
                return true;
            } // if (...)

            value = null;
            return false;
        }
    }

    private readonly Utf8JsonParser<T>? _valueParser;

    public NullableNoexceptConverter()
    {
    }

    public NullableNoexceptConverter(Utf8JsonParser<T> valueParser)
    {
        ArgumentNullException.ThrowIfNull(valueParser);
        _valueParser = valueParser;
    }

    /// <summary>
    /// Fetches the explicit value parser or retrieves the cached one if none was provided.
    /// </summary>
    private Utf8JsonParser<T> GetValueParser(NullabilityAwareType<T?> typeToConvert)
    {
        if (_valueParser is not null)
            return _valueParser;

        // Extract T from Nullable<T>.
        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[0];

        if (!NoexceptJson.TryMakeParser(valueType, out Utf8JsonParser<T>? valueParser))
            throw new NotSupportedException("Converter for T in Nullable`[T] not found.");

        return valueParser;
    }

    public override Utf8JsonParser<T?> MakeParser(NullabilityAwareType<T?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        Medium medium = new(this.GetValueParser(typeToConvert));

        return medium.TryGet;
    }
}
