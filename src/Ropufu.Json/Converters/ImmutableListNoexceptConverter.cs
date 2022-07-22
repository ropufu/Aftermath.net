using System.Text.Json;

namespace Ropufu.Json;

public sealed class ImmutableListNoexceptConverter<T>
    : NoexceptJsonConverter<ImmutableList<T?>?>
{
    private class Medium
    {
        private readonly Utf8JsonParser<List<T?>?> _listParser;

        public Medium(Utf8JsonParser<List<T?>?> listReader)
            => _listParser = listReader;

        public bool TryGet(ref Utf8JsonReader json, out ImmutableList<T?>? value)
        {
            if (_listParser(ref json, out List<T?>? intermediate))
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

    private readonly Utf8JsonParser<T?>? _valueParser;

    public bool DoAllowSingleton { get; set; }

    public ImmutableListNoexceptConverter()
        : this(false)
    {
    }

    public ImmutableListNoexceptConverter(bool doAllowSingleton)
        => this.DoAllowSingleton = doAllowSingleton;

    public ImmutableListNoexceptConverter(Utf8JsonParser<T?> valueParser)
        : this(valueParser, false)
    {
    }

    public ImmutableListNoexceptConverter(Utf8JsonParser<T?> valueParser, bool doAllowSingleton)
    {
        ArgumentNullException.ThrowIfNull(valueParser);

        _valueParser = valueParser;
        this.DoAllowSingleton = doAllowSingleton;
    }

    public override Utf8JsonParser<ImmutableList<T?>?> MakeParser(NullabilityAwareType<ImmutableList<T?>?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        ListNoexceptConverter<T?> listConverter =
            _valueParser is null
            ? new(this.DoAllowSingleton)
            : new(_valueParser, this.DoAllowSingleton);

        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[0]!;
        NullabilityAwareType<List<T?>?> listType = NullabilityAwareType.MakeGenericType<List<T?>?>(typeToConvert.State, valueType);

        Medium medium = new(listConverter.MakeParser(listType));

        return medium.TryGet;
    }
}

public sealed class ImmutableListNoexceptConverterFactory<TAllowSingelton>
    : NoexceptJsonConverterFactory
    where TAllowSingelton : Indicator
{
    private static readonly bool s_doAllowSingleton = typeof(TAllowSingelton) == typeof(TrueType);

    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!typeToConvert.IsGenericType)
            return false;

        if (typeToConvert.GetGenericTypeDefinition() != typeof(ImmutableList<>))
            return false;

        return true;
    }

    public override NoexceptJsonConverter CreateConverter(NullabilityAwareType typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!this.CanConvert(typeToConvert.Type))
            throw new NotSupportedException("ImmutableList`[T] expected.");

        NullabilityAwareType valueType = typeToConvert.GetGenericArguments()[0];

        Type converterType = typeof(ImmutableListNoexceptConverter<>).MakeGenericType(valueType.Type);
        return (NoexceptJsonConverter)Activator.CreateInstance(
            converterType,
            new object[] { s_doAllowSingleton })!;
    }
}
