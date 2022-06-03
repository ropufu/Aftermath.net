using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("title")]
    public string? Title { get; private set; }

    [JsonInclude]
    [JsonPropertyName("description")]
    public string? Description { get; private set; }

    [JsonInclude]
    [JsonPropertyName("default")]
    public JsonElement DefaultValue { get; private set; }

    [JsonInclude]
    [JsonPropertyName("deprecated")]
    public bool IsDeprecated { get; private set; } = false;

    [JsonInclude]
    [JsonPropertyName("readOnly")]
    public bool IsReadOnly { get; private set; } = false;

    [JsonInclude]
    [JsonPropertyName("writeOnly")]
    public bool IsWriteOnly { get; private set; } = false;

    [JsonInclude]
    [JsonPropertyName("examples")]
    public ImmutableList<JsonElement> Examples { get; private set; } = new();

    //private void InitializeMetadataBlock()
    //{
    //}
}
