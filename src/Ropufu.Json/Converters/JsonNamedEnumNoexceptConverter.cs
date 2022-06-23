using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public class JsonNamedEnumNoexceptConverter<TEnum>
    : NoexceptJsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public static readonly bool IsFlags = typeof(TEnum).GetCustomAttribute<FlagsAttribute>(false) is not null;
    private static readonly Type s_underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
    private static readonly SortedList<string, long> s_name_lookup = new();
    private static readonly Utf8JsonParser<string> s_singletonParser;
    private static readonly Utf8JsonParser<List<string>> s_listParser;

    static JsonNamedEnumNoexceptConverter()
    {
        Type enumType = typeof(TEnum);

        foreach (FieldInfo info in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            JsonPropertyNameAttribute? nameAttribute = info.GetCustomAttribute<JsonPropertyNameAttribute>(false);
            JsonIgnoreAttribute? ignoreAttribute = info.GetCustomAttribute<JsonIgnoreAttribute>(false);

            if (nameAttribute is null)
                continue;

            if ((ignoreAttribute is not null) && (ignoreAttribute.Condition == JsonIgnoreCondition.Always))
                continue;

            object underlying = info.GetRawConstantValue()!;
            long promoted = Convert.ToInt64(underlying);

            s_name_lookup.Add(nameAttribute.Name, promoted);
        } // foreach (...)

        NullabilityAwareType<string> stringType = NullabilityAwareType.MakeSimple<string>(NullabilityState.NotNull);
        NullabilityAwareType<List<string?>?> stringListType = NullabilityAwareType.MakeGenericType<List<string?>?>(NullabilityState.NotNull, stringType);

        NoexceptJson.TryMakeParser(stringType, out s_singletonParser!);

        ListNoexceptConverter<string> listConverter = new(doAllowSingleton: true);
        s_listParser = listConverter.MakeParser(stringListType)!;
    }

    public static bool TryGetName(TEnum value, [MaybeNullWhen(returnValue: false)] out string name)
    {
        long promoted = Convert.ToInt64(value);

        foreach (KeyValuePair<string, long> x in s_name_lookup)
        {
            if (x.Value == promoted)
            {
                name = x.Key;
                return true;
            } // if (...)
        } // foreach (...)

        name = default;
        return false;
    }

    public static bool TryGetNames(TEnum value, [MaybeNullWhen(returnValue: false)] out List<string> names)
    {
        names = new(capacity: s_name_lookup.Count);
        long promoted = Convert.ToInt64(value);
        long reconstructed = 0;

        foreach (KeyValuePair<string, long> x in s_name_lookup)
        {
            if ((x.Value & promoted) == 0)
                continue;

            names.Add(x.Key);
            reconstructed |= x.Value;
        } // foreach (...)

        if (reconstructed != promoted)
        {
            names = null;
            return false;
        } // if (...)

        return true;
    }

    public static bool TryParse(string name, out TEnum result)
    {
        result = default;

        if (!s_name_lookup.TryGetValue(name, out long promoted))
            return false;

        result = (TEnum)Convert.ChangeType(promoted, s_underlyingType);
        return true;
    }

    public static bool TryParse(IEnumerable<string> names, out TEnum result)
    {
        result = default;

        long aggregate = 0;
        foreach (string x in names)
            if (s_name_lookup.TryGetValue(x, out long promoted))
                aggregate |= promoted;
            else
                return false;

        result = (TEnum)Convert.ChangeType(aggregate, s_underlyingType);
        return true;
    }

    private static bool TryGetNonFlagEnum(ref Utf8JsonReader json, out TEnum value)
    {
        if (s_singletonParser(ref json, out string? name))
            return JsonNamedEnumNoexceptConverter<TEnum>.TryParse(name!, out value);
        else
        {
            value = default;
            return false;
        } // else
    }

    private static bool TryGetFlagEnum(ref Utf8JsonReader json, out TEnum value)
    {
        if (s_listParser(ref json, out List<string>? names))
            return JsonNamedEnumNoexceptConverter<TEnum>.TryParse(names!, out value);
        else
        {
            value = default;
            return false;
        } // else
    }

    public override Utf8JsonParser<TEnum> MakeParser(NullabilityAwareType<TEnum> typeToConvert)
        => JsonNamedEnumNoexceptConverter<TEnum>.MakeParser();

    public static Utf8JsonParser<TEnum> MakeParser()
        => JsonNamedEnumNoexceptConverter<TEnum>.IsFlags
            ? JsonNamedEnumNoexceptConverter<TEnum>.TryGetFlagEnum
            : JsonNamedEnumNoexceptConverter<TEnum>.TryGetNonFlagEnum;
}
