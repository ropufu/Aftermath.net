using System.Text.Json.Serialization;

namespace Ropufu.Json;

[Flags]
[JsonConverter(typeof(JsonNamedEnumConverter<SimpleType>))]
[NoexceptJsonConverter(typeof(JsonNamedEnumNoexceptConverter<SimpleType>))]
public enum SimpleType
{
    Missing = 0,
    [JsonPropertyName("array")]
    Array = 0x01,
    [JsonPropertyName("boolean")]
    Boolean = 0x02,
    [JsonPropertyName("integer")]
    Integer = 0x04,
    [JsonPropertyName("null")]
    Null = 0x08,
    [JsonPropertyName("number")]
    Number = 0x10,
    [JsonPropertyName("object")]
    Object = 0x20,
    [JsonPropertyName("string")]
    String = 0x40
}
