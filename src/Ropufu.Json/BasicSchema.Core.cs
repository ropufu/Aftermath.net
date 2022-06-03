using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    private static readonly Regex s_idValidator = new("^[^#]*#?$", RegexOptions.Compiled);
    private static readonly Regex s_anchorValidator = new("^[A-Za-z_][-A-Za-z0-9._]*$", RegexOptions.Compiled);

    [JsonInclude]
    [JsonPropertyName("$id")]
    public string? IdReference { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$schema")]
    public string? SchemaReference { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$ref")]
    public string? Reference { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$anchor")]
    public string? Anchor { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$dynamicRef")]
    public string? DynamicReference { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$dynamicAnchor")]
    public string? DynamicAnchor { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$vocabulary")]
    public ImmutableDictionary<string, bool> Vocabulary { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("$comment")]
    public string? Comment { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$defs")]
    public ImmutableDictionary<string, TSchema> Definitions { get; private set; } = new();

    private void InitializeCoreBlock()
    {
        if (this.IdReference is not null)
        {
            if (!Uri.TryCreate(this.IdReference, UriKind.RelativeOrAbsolute, out _))
                this.LogError(Literals.ExpectedUriReference, s_jsonNames[nameof(this.IdReference)]);

            if (!s_idValidator.IsMatch(this.IdReference))
                this.LogError("Non-empty fragments not allowed.", s_jsonNames[nameof(this.IdReference)]);
        } // if (...)

        if (this.SchemaReference is not null && !Uri.TryCreate(this.SchemaReference, UriKind.Absolute, out _))
            this.LogError(Literals.ExpectedAbsoluteUri, s_jsonNames[nameof(this.SchemaReference)]);

        if (this.Reference is not null && !Uri.TryCreate(this.Reference, UriKind.RelativeOrAbsolute, out _))
            this.LogError(Literals.ExpectedAbsoluteUri, s_jsonNames[nameof(this.Reference)]);

        if (this.DynamicReference is not null && !Uri.TryCreate(this.DynamicReference, UriKind.RelativeOrAbsolute, out _))
            this.LogError(Literals.ExpectedAbsoluteUri, s_jsonNames[nameof(this.DynamicReference)]);

        if (this.Anchor is not null && !s_anchorValidator.IsMatch(this.Anchor))
            this.LogError(Literals.NotRecognized, s_jsonNames[nameof(this.Anchor)]);

        if (this.DynamicAnchor is not null && !s_anchorValidator.IsMatch(this.DynamicAnchor))
            this.LogError(Literals.NotRecognized, s_jsonNames[nameof(this.DynamicAnchor)]);

        foreach (string key in this.Vocabulary.Keys)
            if (!Uri.TryCreate(key, UriKind.RelativeOrAbsolute, out _))
                this.LogError(Literals.ExpectedUriReference, s_jsonNames[nameof(this.Vocabulary)]);
    }
}
