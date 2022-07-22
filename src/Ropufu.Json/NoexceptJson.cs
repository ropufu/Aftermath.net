using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ropufu.Json;

public delegate bool Utf8JsonParser<T>(ref Utf8JsonReader json, out T? value);

public static partial class NoexceptJson
{
    private static readonly Dictionary<NullabilityAwareType, Delegate> s_knownParsers = new();

    static NoexceptJson()
    {
        /* Primitive types. */
        NoexceptJson.CacheSimpleParser<bool>(NoexceptJson.TryGetBoolean, NoexceptJson.TryGetBoolean);
        NoexceptJson.CacheSimpleParser<byte>(NoexceptJson.TryGetByte, NoexceptJson.TryGetByte);
        NoexceptJson.CacheSimpleParser<short>(NoexceptJson.TryGetInt16, NoexceptJson.TryGetInt16);
        NoexceptJson.CacheSimpleParser<int>(NoexceptJson.TryGetInt32, NoexceptJson.TryGetInt32);
        NoexceptJson.CacheSimpleParser<long>(NoexceptJson.TryGetInt64, NoexceptJson.TryGetInt64);
        NoexceptJson.CacheSimpleParser<nint>(NoexceptJson.TryGetIntPtr, NoexceptJson.TryGetIntPtr);
        NoexceptJson.CacheSimpleParser<float>(NoexceptJson.TryGetSingle, NoexceptJson.TryGetSingle);
        NoexceptJson.CacheSimpleParser<double>(NoexceptJson.TryGetDouble, NoexceptJson.TryGetDouble);
        NoexceptJson.CacheSimpleParser<char>(NoexceptJson.TryGetChar, NoexceptJson.TryGetChar);

        /* Common structs. */
        NoexceptJson.CacheSimpleParser<decimal>(NoexceptJson.TryGetDecimal, NoexceptJson.TryGetDecimal);
        NoexceptJson.CacheSimpleParser<Guid>(NoexceptJson.TryGetGuid, NoexceptJson.TryGetGuid);
        NoexceptJson.CacheSimpleParser<DateTime>(NoexceptJson.TryGetDateTime, NoexceptJson.TryGetDateTime);
        NoexceptJson.CacheSimpleParser<DateTimeOffset>(NoexceptJson.TryGetDateTimeOffset, NoexceptJson.TryGetDateTimeOffset);
        NoexceptJson.CacheSimpleParser<JsonElement>(NoexceptJson.TryGetJsonElement);

        /* Common classes. */
        NoexceptJson.CacheSimpleParser<string>(NoexceptJson.TryGetNotNullString, NoexceptJson.TryGetNullableString);
        NoexceptJson.CacheSimpleParser<Regex>(NoexceptJson.TryGetNotNullRegex, NoexceptJson.TryGetNullableRegex);
    }

    private static void AddParsers<T>(NullabilityAwareType<T> valueType, Utf8JsonParser<T> valueParser)
    {
        NullabilityAwareType<string> stringType = NullabilityAwareType.MakeSimple<string>(NullabilityState.NotNull);

        ListNoexceptConverter<T> listConverter = new(valueParser!);
        ArrayNoexceptConverter<T> arrayConverter = new(valueParser!);
        DictionaryNoexceptConverter<T> dictionaryConverter = new(valueParser!);

        s_knownParsers.Add(valueType, valueParser);

        foreach (NullabilityState x in Enum.GetValues<NullabilityState>())
        {
            NullabilityAwareType<List<T?>?> listType = NullabilityAwareType.MakeGenericType<List<T?>?>(x, valueType);
            s_knownParsers.Add(listType, listConverter.MakeParser(listType));

            NullabilityAwareType<T?[]?> arrayType = valueType.MakeArrayType(x).Promote<T?[]?>();
            s_knownParsers.Add(arrayType, arrayConverter.MakeParser(arrayType));

            NullabilityAwareType<Dictionary<string, T?>?> dictionaryType = NullabilityAwareType.MakeGenericType<Dictionary<string, T?>?>(x, stringType, valueType);
            s_knownParsers.Add(dictionaryType, dictionaryConverter.MakeParser(dictionaryType));
        } // foreach (...)
    }

    private static void CacheSimpleParser<T>(Utf8JsonParser<T> notNullParser)
        where T : struct
    {
        NullabilityAwareType<T> type = NullabilityAwareType.MakeSimple<T>();
        NoexceptJson.AddParsers(type, notNullParser);
    }

    private static void CacheSimpleParser<T>(Utf8JsonParser<T> notNullParser, Utf8JsonParser<T?> nullablePraser)
        where T : struct
    {
        NullabilityAwareType<T> notNullType = NullabilityAwareType.MakeSimple<T>();
        NullabilityAwareType<T?> nullableType = NullabilityAwareType.MakeNullable<T>();
        NoexceptJson.AddParsers(notNullType, notNullParser);
        NoexceptJson.AddParsers(nullableType, nullablePraser);
    }

    private static void CacheSimpleParser<T>(Utf8JsonParser<T> notNullParser, Utf8JsonParser<T> fallbackParser)
        where T : class
    {
        foreach (NullabilityState valueState in Enum.GetValues<NullabilityState>())
        {
            NullabilityAwareType<T> valueType = NullabilityAwareType.MakeSimple<T>(valueState);
            Utf8JsonParser<T> valueParser = valueType.IsNotNull ? notNullParser : fallbackParser;
            NoexceptJson.AddParsers(valueType, valueParser);
        } // foreach (...)
    }

    /// <exception cref="ArgumentException">Nullability-aware type inconsistent with T.</exception>
    public static bool TryRegisterParser<T>(NullabilityAwareType<T> typeToParse, Utf8JsonParser<T> parser)
    {
        ArgumentNullException.ThrowIfNull(typeToParse);
        ArgumentNullException.ThrowIfNull(parser);

        if (typeToParse.Type != typeof(T))
            throw new ArgumentException("Nullability-aware type inconsistent with T.", nameof(typeToParse));

        return s_knownParsers.TryAdd(typeToParse, parser);
    }

    /// <exception cref="ArgumentNullException">Type cannot be null.</exception>
    /// <exception cref="ArgumentException">Type cannot be a generic definition or a generic parameter.</exception>
    /// <exception cref="NotSupportedException">Custom noexcept JSON converter malformed.</exception>
    public static bool TryMakeParser<T>(NullabilityAwareType<T> typeToParse, [MaybeNullWhen(returnValue: false)] out Utf8JsonParser<T> parser)
    {
        ArgumentNullException.ThrowIfNull(typeToParse);

        if (NoexceptJson.TryMakeParser(typeToParse, out Delegate? x))
        {
            parser = (Utf8JsonParser<T>)x;
            return true;
        } // if (...)
        else
        {
            parser = null;
            return false;
        } // else
    }

    internal static bool TryMakeParser(NullabilityAwareType typeToParse, [MaybeNullWhen(returnValue: false)] out Delegate parser)
    {
        Type type = typeToParse.Type;

        // Cached types.
        if (s_knownParsers.TryGetValue(typeToParse, out parser))
            return true;
        // Uncached nullable structs.
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            NullabilityAwareType valueType = typeToParse.GetGenericArguments()[0];
            return NoexceptJson.TryMakeParserViaExtensionConverter(
                typeToParse,
                valueType,
                typeof(NullableNoexceptConverter<>),
                out parser);
        } // else if (...)
        // Uncached lists.
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            NullabilityAwareType valueType = typeToParse.GetGenericArguments()[0];
            return NoexceptJson.TryMakeParserViaExtensionConverter(
                typeToParse,
                valueType,
                typeof(ListNoexceptConverter<>),
                out parser);
        } // else if (...)
        // Uncached arrays.
        else if (type.IsArray)
        {
            NullabilityAwareType valueType = typeToParse.GetElementType()!;
            return NoexceptJson.TryMakeParserViaExtensionConverter(
                typeToParse,
                valueType,
                typeof(ArrayNoexceptConverter<>),
                out parser);
        } // else if (...)
        // Check to see if the type has a converter.
        else
            return NoexceptJson.TryMakeParserViaCustomConverter(
                typeToParse,
                out parser);
    }

    private static bool TryMakeParserViaExtensionConverter(
        NullabilityAwareType typeToParse,
        NullabilityAwareType valueType,
        Type converterTypeDefinition,
        [MaybeNullWhen(returnValue: false)] out Delegate parser)
    {
        if (NoexceptJson.TryMakeParser(valueType, out Delegate? valueParser))
        {
            Type converterType = converterTypeDefinition.MakeGenericType(valueType.Type);
            NoexceptJsonConverter converter = (NoexceptJsonConverter)Activator.CreateInstance(converterType, new object[] { valueParser })!;

            parser = converter.MakeUtf8JsonParser(typeToParse);
            s_knownParsers.Add(typeToParse, parser);
            return true;
        } // if (...)

        parser = null;
        return false;
    }

    private static bool TryMakeParserViaCustomConverter(
        NullabilityAwareType typeToParse,
        [MaybeNullWhen(returnValue: false)] out Delegate parser)
    {
        Type type = typeToParse.Type;
        NoexceptJsonConverterAttribute? converterAttribute = type.GetCustomAttribute<NoexceptJsonConverterAttribute>();

        if (converterAttribute is not null && converterAttribute.CanConvert(typeToParse.Type))
        {
            parser = converterAttribute.MakeUtf8JsonParser(typeToParse);
            s_knownParsers.Add(typeToParse, parser);
            return true;
        } // if (...)

        parser = null;
        return false;
    }
}
