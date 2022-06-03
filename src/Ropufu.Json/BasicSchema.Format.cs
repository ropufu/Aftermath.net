using System.Text.Json.Serialization;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("format")]
    public string? Format { get; private set; }

    //private void InitializeFormatBlock()
    //{
    //}
}
