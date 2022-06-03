using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public class JsonNamedEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Utf8JsonParser<TEnum> s_noexceptParser = JsonNamedEnumNoexceptConverter<TEnum>.MakeParser();

    public override bool HandleNull => true;

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (s_noexceptParser(ref reader, out TEnum result))
            return result;
        else
            throw new JsonException("Reading enum value failed.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        if (!JsonNamedEnumNoexceptConverter<TEnum>.IsFlags)
        {
            if (JsonNamedEnumNoexceptConverter<TEnum>.TryGetName(value, out string? name))
                writer.WriteStringValue(name);
            else
                throw new JsonException("Value not recongnized.");
        }
        else
        {
            if (!JsonNamedEnumNoexceptConverter<TEnum>.TryGetNames(value, out List<string>? names))
                throw new JsonException("Value could not be broken down into named pieces.");

            switch (names.Count)
            {
                case 1:
                    writer.WriteStringValue(names[0]);
                    break;
                default:
                    writer.WriteStartArray();

                    foreach (string x in names)
                        writer.WriteStringValue(x);

                    writer.WriteEndArray();
                    break;
            } // switch (...)
        } // else
    }
}
