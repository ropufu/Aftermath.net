using System.Text.Json;

namespace Ropufu.Json;

public sealed class ArrayNoexceptConverter<T>
    : NoexceptJsonConverter<T?[]?>
{
    private class Medium
    {
        private readonly Utf8JsonParser<List<T?>?> _listParser;

        public Medium(Utf8JsonParser<List<T?>?> listParser)
            => _listParser = listParser;

        public bool TryGet(ref Utf8JsonReader json, out T?[]? value)
        {
            if (_listParser(ref json, out List<T?>? intermediate))
            {
                value = intermediate?.ToArray();
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

    public ArrayNoexceptConverter(bool doAllowSingleton = false)
        => this.DoAllowSingleton = doAllowSingleton;

    public ArrayNoexceptConverter(Utf8JsonParser<T?> valueParser, bool doAllowSingleton = false)
    {
        ArgumentNullException.ThrowIfNull(valueParser);

        _valueParser = valueParser;
        this.DoAllowSingleton = doAllowSingleton;
    }

    public override Utf8JsonParser<T?[]?> MakeParser(NullabilityAwareType<T?[]?> typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        ListNoexceptConverter<T?> listConverter =
            _valueParser is null
            ? new(this.DoAllowSingleton)
            : new(_valueParser, this.DoAllowSingleton);

        NullabilityAwareType valueType = typeToConvert.GetElementType()!;
        NullabilityAwareType<List<T?>?> listType = NullabilityAwareType.MakeGenericType<List<T?>?>(typeToConvert.State, valueType);

        Medium medium = new(listConverter.MakeParser(listType));

        return medium.TryGet;
    }
}
