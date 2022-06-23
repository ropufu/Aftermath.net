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
    public string? StaticReference { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$anchor")]
    public string? StaticAnchor { get; private set; }

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
                this.Log(Literals.ExpectedUriReference, MessageLevel.Error, s_jsonPointers[nameof(this.IdReference)]);

            if (!s_idValidator.IsMatch(this.IdReference))
                this.Log("Non-empty fragments not allowed.", MessageLevel.Error, s_jsonPointers[nameof(this.IdReference)]);
        } // if (...)

        // Bending rules: allow UriKind.RelativeOrAbsolute rather than UriKind.Absolute schema references.
        if (this.SchemaReference is not null && !Uri.TryCreate(this.SchemaReference, UriKind.RelativeOrAbsolute, out _))
            this.Log(Literals.ExpectedUriReference, MessageLevel.Error, s_jsonPointers[nameof(this.SchemaReference)]);

        if (this.StaticReference is not null)
        {
            if (!Uri.TryCreate(this.StaticReference, UriKind.RelativeOrAbsolute, out _))
                this.Log(Literals.ExpectedUriReference, MessageLevel.Error, s_jsonPointers[nameof(this.StaticReference)]);

            if (this.StaticReference.StartsWith('#') && !JsonPointer.TryParse("/" + this.StaticReference, out _))
                this.Log(Literals.InvalidJsonReference, MessageLevel.Error, s_jsonPointers[nameof(this.StaticReference)]);
        } // if (...)

        if (this.DynamicReference is not null)
        {
            if (!Uri.TryCreate(this.DynamicReference, UriKind.RelativeOrAbsolute, out _))
                this.Log(Literals.ExpectedUriReference, MessageLevel.Error, s_jsonPointers[nameof(this.DynamicReference)]);

            if (this.DynamicReference.StartsWith('#') && !JsonPointer.TryParse("/" + this.DynamicReference, out _))
                this.Log(Literals.InvalidJsonReference, MessageLevel.Error, s_jsonPointers[nameof(this.DynamicReference)]);
        } // if (...)

        if (this.StaticReference is not null && this.DynamicReference is not null)
            this.Log(
                "Cannot have both static and dynamic reference set simultaneously.",
                MessageLevel.Error);

        if (this.StaticAnchor is not null && !s_anchorValidator.IsMatch(this.StaticAnchor))
            this.Log(Literals.NotRecognized, MessageLevel.Error, s_jsonPointers[nameof(this.StaticAnchor)]);

        if (this.DynamicAnchor is not null && !s_anchorValidator.IsMatch(this.DynamicAnchor))
            this.Log(Literals.NotRecognized, MessageLevel.Error, s_jsonPointers[nameof(this.DynamicAnchor)]);

        foreach (string key in this.Vocabulary.Keys)
            if (!Uri.TryCreate(key, UriKind.RelativeOrAbsolute, out _))
                this.Log(Literals.ExpectedUriReference, MessageLevel.Error, s_jsonPointers[nameof(this.Vocabulary)]);
    }
}
