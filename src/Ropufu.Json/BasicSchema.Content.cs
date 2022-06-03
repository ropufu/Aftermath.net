using System.Text.Json.Serialization;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("contentEncoding")]
    public string? ContentEncoding { get; private set; }

    [JsonInclude]
    [JsonPropertyName("contentMediaType")]
    public string? ContentMediaType { get; private set; }

    [JsonInclude]
    [JsonPropertyName("contentSchema")]
    public TSchema ContentSchema { get; private set; } = BasicSchema<TSchema>.TrivialTrue;

    //private void InitializeContentBlock()
    //{
    //}
}
