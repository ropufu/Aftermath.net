using System.Text.Json.Serialization;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("unevaluatedItems")]
    public TSchema? UnevaluatedArrayItems { get; private set; }

    [JsonInclude]
    [JsonPropertyName("unevaluatedProperties")]
    public TSchema? UnevaluatedProperties { get; private set; }

    //private void InitializeUnevaluatedBlock()
    //{
    //}
}
