using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public class ImmutableDictionaryConverter<TKey, TValue>
    : JsonConverter<ImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    public override bool HandleNull => false;

    public override ImmutableDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(Literals.ExpectedObject);

        Dictionary<TKey, TValue> dictionary = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options)!;

        return new(dictionary);
    }

    public override void Write(Utf8JsonWriter writer, ImmutableDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        JsonSerializer.Serialize(writer, value.ToDictionary(), options);
    }
}

public class ImmutableDictionaryConverterFactory
    : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!typeToConvert.IsGenericType)
            return false;

        if (typeToConvert.GetGenericTypeDefinition() != typeof(ImmutableDictionary<,>))
            return false;

        return true;
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        Type keyType = typeToConvert.GetGenericArguments()[0];
        Type valueType = typeToConvert.GetGenericArguments()[1];
        Type converterType = typeof(ImmutableDictionaryConverter<,>).MakeGenericType(keyType, valueType);

        JsonConverter converter = (JsonConverter)Activator.CreateInstance(converterType)!;

        return converter;
    }
}
