using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Ropufu.Json;

public class DictionaryNoexceptConverter<TValue>
    : NoexceptJsonConverter<Dictionary<string, TValue?>?>
{
    private class Medium
    {
        private readonly Utf8JsonParser<TValue?> _valueParser;

        public Medium(Utf8JsonParser<TValue?> valueParser)
            => _valueParser = valueParser;

        public bool TryGetNotNull(ref Utf8JsonReader json, [MaybeNullWhen(returnValue: false)] out Dictionary<string, TValue?> value)
        {
            switch (json.TokenType)
            {
                case JsonTokenType.StartObject:
                    return this.TryGetUnchecked(ref json, out value);
                default:
                    value = null;
                    return false;
            } // switch (...)
        }

        public bool TryGetMaybeNull(ref Utf8JsonReader json, out Dictionary<string, TValue?>? value)
        {
            switch (json.TokenType)
            {
                case JsonTokenType.Null:
                    value = null;
                    return true;
                case JsonTokenType.StartObject:
                    return this.TryGetUnchecked(ref json, out value);
                default:
                    value = null;
                    return false;
            } // switch (...)
        }

        private bool TryGetUnchecked(ref Utf8JsonReader json, out Dictionary<string, TValue?>? value)
        {
            bool isGood = true;
            value = new();

            while (json.Read() && json.TokenType != JsonTokenType.EndObject)
            {
                if (json.TokenType != JsonTokenType.PropertyName)
                    return false;

                string? propertyName = json.GetString();

                if (propertyName is null)
                    isGood = false;

                // Move to property value.
                if (!json.Read())
                    return false;

                // Skip unrecognized properties.
                if (propertyName is null)
                {
                    json.Skip();
                    continue;
                } // if (...)

                // Duplicate property name encountered.
                if (value.ContainsKey(propertyName))
                {
                    isGood = false;
                    json.Skip();
                    continue;
                } // if (...)

                if (!_valueParser(ref json, out TValue? propertyValue))
                {
                    isGood = false;
                    json.Skip();
                    continue;
                } // if (...)

                value.Add(propertyName, propertyValue);
            } // for (...)

            return isGood;
        }
    }

    private readonly Utf8JsonParser<TValue?>? _valueParser;

    public DictionaryNoexceptConverter()
    {
    }

    public DictionaryNoexceptConverter(Utf8JsonParser<TValue?> valueParser)
    {
        ArgumentNullException.ThrowIfNull(valueParser);
        _valueParser = valueParser;
    }

    /// <summary>
    /// Fetches the explicit value parser or retrieves the cached one if none was provided.
    /// </summary>
    private Utf8JsonParser<TValue?> GetValueParser(NullabilityAwareType<Dictionary<string, TValue?>?> typeToConvert)
    {
        if (_valueParser is not null)
            return _valueParser;

        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[1];

        if (!NoexceptJson.TryMakeParser(valueType, out Utf8JsonParser<TValue?>? valueParser))
            throw new NotSupportedException("Converter for T in Dictionary`[string, T] not found.");

        return valueParser;
    }

    public override Utf8JsonParser<Dictionary<string, TValue?>?> MakeParser(NullabilityAwareType<Dictionary<string, TValue?>?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        Medium medium = new(this.GetValueParser(typeToConvert));

        return typeToConvert.IsNotNull
            ? medium.TryGetNotNull
            : medium.TryGetMaybeNull;
    }
}
