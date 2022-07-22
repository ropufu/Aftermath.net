using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public class ImmutableListConverter<T>
    : JsonConverter<ImmutableList<T?>>
{
    private static readonly Utf8JsonParser<ImmutableList<T?>?> s_noexceptParser;

    static ImmutableListConverter()
    {
        NullabilityAwareType<ImmutableList<T?>?> typeToConvert = NullabilityAwareType<ImmutableList<T?>?>.Unknown();

        ImmutableListNoexceptConverter<T> noexceptConverter = new();
        s_noexceptParser = noexceptConverter.MakeParser(typeToConvert);
    }

    public ImmutableListConverter()
        : this(false)
    {
    }

    public ImmutableListConverter(bool doAllowSingleton)
        => this.DoAllowSingleton = doAllowSingleton;

    public bool DoAllowSingleton { get; set; }

    public override bool HandleNull => false;

    public override ImmutableList<T?>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (s_noexceptParser(ref reader, out ImmutableList<T?>? result))
            return result;
        else
            throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ImmutableList<T?> value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        if (this.DoAllowSingleton && value.Count == 1)
            JsonSerializer.Serialize(writer, value[0], options);
        else
            JsonSerializer.Serialize(writer, value.ToReadOnly(), options);
    }
}

public class ImmutableListConverterFactory<TAllowSingelton>
    : JsonConverterFactory
    where TAllowSingelton : Indicator
{
    private static readonly bool s_doAllowSingleton = typeof(TAllowSingelton) == typeof(TrueType);
    private static readonly ImmutableListNoexceptConverterFactory<TAllowSingelton> s_noexceptFactory = new();

    public override bool CanConvert(Type typeToConvert)
        => s_noexceptFactory.CanConvert(typeToConvert);

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        Type valueType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(ImmutableListConverter<>).MakeGenericType(valueType);

        JsonConverter converter = (JsonConverter)Activator.CreateInstance(
            converterType,
            new object[] { s_doAllowSingleton })!;

        return converter;
    }
}
