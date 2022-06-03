using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

public sealed class Schema
    : BasicSchema<Schema>
{
    protected override bool IsMatchOverride(ref JsonElement element)
        => true;

    public static implicit operator Schema(bool trivialValue)
        => trivialValue ? Schema.TrivialTrue : Schema.TrivialFalse;
}

/// <summary>
/// Provides base for non-throwing JSON schemas. Rather than rely on JsonException when
/// parsing fails, keeps track of errors by means of Verbose functionality.
/// Properties not marked with [JsonPropertyName] are ignored.
/// </summary>
/// <typeparam name="TSchema">Implementation of BasicSchema.</typeparam>
[NoexceptJsonConverter(typeof(BasicSchemaNoexceptConverterFactory))]
public abstract partial class BasicSchema<TSchema>
    : Verbose, IResponsiveNoexceptJson
    where TSchema : BasicSchema<TSchema>, new()
{
    public static readonly TSchema TrivialTrue = new() { IsTrivialTrue = true };
    public static readonly TSchema TrivialFalse = new() { IsTrivialFalse = true };

    private static readonly IReadOnlyDictionary<string, string> s_jsonNames = JsonObjectNoexceptConverter<TSchema>.JsonNames;

    [JsonInclude]
    [JsonPropertyName("definitions")]
    public JsonElement DeprecatedDefinitions { get; private set; }

    [JsonInclude]
    [JsonPropertyName("dependencies")]
    public JsonElement DeprecatedDependencies { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$recursiveAnchor")]
    public JsonElement DeprecatedRecursiveAnchor { get; private set; }

    [JsonInclude]
    [JsonPropertyName("$recursiveRef")]
    public JsonElement DeprecatedRecursiveReference { get; private set; }

    public bool IsTrivialTrue { get; private init; } = false;

    public bool IsTrivialFalse { get; private init; } = false;

    /// <exception cref="ArgumentNullException">JSON document cannot be null.</exception>
    /// <exception cref="NotSupportedException">Cannot validate against malformed schema.</exception>
    public bool IsMatch(JsonDocument json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return this.IsMatch(json.RootElement);
    }

    /// <exception cref="NotSupportedException">Cannot validate against malformed schema.</exception>
    public bool IsMatch(JsonElement element)
        => this.IsMatch(ref element);

    /// <exception cref="NotSupportedException">Cannot validate against malformed schema.</exception>
    public bool IsMatch(ref JsonElement element)
    {
        if (this.Has(ErrorLevel.Error))
            throw new NotSupportedException("Cannot validate against malformed schema.");

        if (this.IsTrivialTrue)
            return true;

        if (this.IsTrivialFalse)
            return false;

        if (!this.IsPermissible(ref element))
            return false;

        SimpleType schemaType = this.Type;

        // Try match against all types.
        if (schemaType == SimpleType.Missing)
        {
            foreach (SimpleType x in Enum.GetValues<SimpleType>())
                if (this.IsMatch(ref element, x))
                    return this.IsMatchOverride(ref element);
        } // if (...)
        // Try match against declared types.
        else
            foreach (SimpleType x in Enum.GetValues<SimpleType>())
                if ((schemaType & x) != SimpleType.Missing)
                    if (this.IsMatch(ref element, x))
                        return this.IsMatchOverride(ref element);

        return false;
    }

    private bool IsMatch(ref JsonElement element, SimpleType singletonType)
    {
        switch (singletonType)
        {
            case SimpleType.Null:
                if (element.ValueKind != JsonValueKind.Null)
                    return false;
                return true;
            case SimpleType.Boolean:
                if (!(element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
                    return false;
                return true;
            case SimpleType.Integer:
                if (element.ValueKind != JsonValueKind.Number)
                    return false;
                if (!element.TryGetInt64(out long integerValue))
                    return false;
                return this.TryValidate(integerValue);
            case SimpleType.Number:
                if (element.ValueKind != JsonValueKind.Number)
                    return false;
                // @todo Implement handling decimals.
                if (!element.TryGetDouble(out double numberValue))
                    return false;
                return this.TryValidate(numberValue);
            case SimpleType.String:
                if (element.ValueKind != JsonValueKind.String)
                    return false;
                return this.TryValidate(element.GetString()!);
            case SimpleType.Object:
                if (element.ValueKind != JsonValueKind.Object)
                    return false;
                return this.TryValidateObject(ref element);
            case SimpleType.Array:
                if (element.ValueKind != JsonValueKind.Array)
                    return false;
                return this.TryValidateArray(ref element);
            default:
                return false;
        } // switch (...)
    }

    /// <summary>
    /// Called when an element matches the basic schema, so that inheriting schema may
    /// impose further restrictions. The only exception is when the the basic schema
    /// is trivial true (trivial behavior cannot be overridden).
    /// </summary>
    protected abstract bool IsMatchOverride(ref JsonElement element);

    protected virtual void OnDeserializedOverride()
    {
    }

    public void OnDeserialized()
    {
        this.InitializeApplicatorBlock();
        //this.InitializeContentBlock();
        this.InitializeCoreBlock();
        //this.InitializeFormatBlock();
        //this.InitializeMetadataBlock();
        //this.InitializeUnevaluatedBlock();
        this.InitializeValidationBlock();

        JsonElement constantValue = this.ConstantValue;
        ImmutableList<JsonElement>? permissibleValues = this.PermissibleValues;

        // Make the schema incomplete to prevent circular references for constant value (CV) and permissible values (PV).
        this.ConstantValue = new();
        this.PermissibleValues = null;
        this.Examples = new();

        // Validate constant value against incomplete (no CV, no PV) schema.
        if (constantValue.ValueKind != JsonValueKind.Undefined)
            if (!this.IsMatch(constantValue))
                this.LogError(
                    $"Constant value \"{constantValue.GetRawText()}\" does not pass schema validation.",
                    s_jsonNames[nameof(this.ConstantValue)]);

        this.ConstantValue = constantValue;

        // Validate permissible values against incomplete (no PV) schema.
        if (permissibleValues is not null)
            foreach (JsonElement x in permissibleValues)
                if (!this.IsMatch(x))
                    this.LogError(
                        $"Permissible value \"{x.GetRawText()}\" does not pass schema validation.",
                        s_jsonNames[nameof(this.PermissibleValues)]);

        this.PermissibleValues = permissibleValues;

        if (this.ConstantValue.ValueKind != JsonValueKind.Undefined && this.PermissibleValues is not null)
        {
            // Permissible values have already been validated against CV.
            if (this.PermissibleValues.Count == 0)
                this.LogError("Constant value is not present in permissible value list.");
        } // if (...)

        // Parse examples against complete schema.
        foreach (JsonElement x in this.Examples)
            if (!this.IsMatch(x))
                this.LogError(
                    $"Example \"{x.GetRawText()}\" does not pass schema validation.",
                    s_jsonNames[nameof(this.PermissibleValues)]);

        if (this.DeprecatedDefinitions.ValueKind == JsonValueKind.Undefined)
            this.LogWarning(
                "\"definitions\" has been replaced by \"$defs\".",
                s_jsonNames[nameof(this.DeprecatedDefinitions)]);

        if (this.DeprecatedDependencies.ValueKind == JsonValueKind.Undefined)
            this.LogWarning(
                "\"dependencies\" has been split and replaced by \"dependentSchemas\" and \"dependentRequired\" in order to serve their differing semantics.",
                s_jsonNames[nameof(this.DeprecatedDependencies)]);

        if (this.DeprecatedRecursiveAnchor.ValueKind == JsonValueKind.Undefined)
            this.LogWarning(
                "\"$recursiveAnchor\" has been replaced by \"$dynamicAnchor\".",
                s_jsonNames[nameof(this.DeprecatedRecursiveAnchor)]);

        if (this.DeprecatedRecursiveReference.ValueKind == JsonValueKind.Undefined)
            this.LogWarning(
                "\"$recursiveRef\" has been replaced by \"$dynamicRef\".",
                s_jsonNames[nameof(this.DeprecatedRecursiveReference)]);

        this.OnDeserializedOverride();
    }

    public void OnDeserializing()
    {
    }

    public void OnParsingFailure(string jsonPropertyName, ref Utf8JsonReader propertyJson)
    {
        this.LogError($"Parsing property \"{jsonPropertyName}\" failed. Encountered: [{propertyJson.TokenType}].");
    }

    public void OnRequiredPropertyMissing(string jsonPropertyName)
    {
        this.LogError($"Required property \"{jsonPropertyName}\" missing.");
    }
}
