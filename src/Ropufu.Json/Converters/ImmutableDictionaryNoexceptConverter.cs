using System.Reflection;
using System.Text.Json;

namespace Ropufu.Json;

public sealed class ImmutableDictionaryNoexceptConverter<TValue>
    : NoexceptJsonConverter<ImmutableDictionary<string, TValue?>?>
{
    private class Medium
    {
        private readonly Utf8JsonParser<Dictionary<string, TValue?>?> _dictionaryParser;

        public Medium(Utf8JsonParser<Dictionary<string, TValue?>?> dictionaryParser)
            => _dictionaryParser = dictionaryParser;

        public bool TryGet(ref Utf8JsonReader json, out ImmutableDictionary<string, TValue?>? value)
        {
            if (_dictionaryParser(ref json, out Dictionary<string, TValue?>? intermediate))
            {
                value = intermediate is null ? null : new(intermediate);
                return true;
            } // if (...)
            else
            {
                value = null;
                return false;
            } // else
        }
    }

    private readonly Utf8JsonParser<TValue?>? _valueParser;

    public ImmutableDictionaryNoexceptConverter()
    {
    }

    public ImmutableDictionaryNoexceptConverter(Utf8JsonParser<TValue?> valueParser)
    {
        ArgumentNullException.ThrowIfNull(valueParser);
        _valueParser = valueParser;
    }

    public override Utf8JsonParser<ImmutableDictionary<string, TValue?>?> MakeParser(NullabilityAwareType<ImmutableDictionary<string, TValue?>?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        DictionaryNoexceptConverter<TValue?> dictionaryConverter =
            _valueParser is null
            ? new()
            : new(_valueParser);

        NullabilityAwareType stringType = NullabilityAwareType.MakeSimple<string>(NullabilityState.NotNull);
        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[1]!;
        NullabilityAwareType<Dictionary<string, TValue?>?> dictionaryType = NullabilityAwareType.MakeGenericType<Dictionary<string, TValue?>?>(typeToConvert.State, stringType, valueType);

        Medium medium = new(dictionaryConverter.MakeParser(dictionaryType));

        return medium.TryGet;
    }
}

public sealed class ImmutableDictionaryNoexceptConverterFactory
    : NoexceptJsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!typeToConvert.IsGenericType)
            return false;

        if (typeToConvert.GetGenericTypeDefinition() != typeof(ImmutableDictionary<,>))
            return false;

        // @todo Think about implementing conversion for Dictionaries with key convertible from JSON string.
        if (typeToConvert.GetGenericArguments()[0] != typeof(string))
            return false;

        return true;
    }

    public override NoexceptJsonConverter CreateConverter(NullabilityAwareType typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!this.CanConvert(typeToConvert.Type))
            throw new NotSupportedException("ImmutableDictionary`[string, T] expected.");

        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[1];

        Type converterType = typeof(ImmutableDictionaryNoexceptConverter<>).MakeGenericType(valueType.Type);
        return (NoexceptJsonConverter)Activator.CreateInstance(converterType)!;
    }
}
