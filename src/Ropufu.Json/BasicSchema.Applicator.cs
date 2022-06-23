using System.Text.Json.Serialization;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("prefixItems")]
    public ImmutableList<TSchema> ArrayPrefixItemSchemas { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("items")]
    public TSchema? ArrayItemsSchema { get; private set; }

    [JsonInclude]
    [JsonPropertyName("contains")]
    public TSchema? ArrayContainsSchema { get; private set; }

    [JsonInclude]
    [JsonPropertyName("additionalProperties")]
    public TSchema AdditionalPropertiesSchema { get; private set; } = BasicSchema<TSchema>.TrivialTrue;

    [JsonInclude]
    [JsonPropertyName("properties")]
    public ImmutableDictionary<string, TSchema> Properties { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("patternProperties")]
    public ImmutableDictionary<string, TSchema> PatternProperties { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("dependentSchemas")]
    public ImmutableDictionary<string, TSchema> PropertyDependentSchemas { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("propertyNames")]
    public TSchema PropertyNamesSchema { get; private set; } = BasicSchema<TSchema>.TrivialTrue;

    [JsonInclude]
    [JsonPropertyName("if")]
    public TSchema? ConditionIfSchema { get; private set; }

    [JsonInclude]
    [JsonPropertyName("then")]
    public TSchema? ConditionThenSchema { get; private set; }

    [JsonInclude]
    [JsonPropertyName("else")]
    public TSchema? ConditionElseSchema { get; private set; }

    [JsonInclude]
    [JsonPropertyName("allOf")]
    public ImmutableList<TSchema>? AllOfSchemas { get; private set; }

    [JsonInclude]
    [JsonPropertyName("anyOf")]
    public ImmutableList<TSchema>? AnyOfSchemas { get; private set; }

    [JsonInclude]
    [JsonPropertyName("oneOf")]
    public ImmutableList<TSchema>? OneOfSchemas { get; private set; }

    [JsonInclude]
    [JsonPropertyName("not")]
    public TSchema? ConditionNotSchema { get; private set; }

    private void InitializeApplicatorBlock()
    {
        foreach (string x in this.PatternProperties.Keys)
            if (!x.IsRegex())
                this.Log(Literals.ExpectedRegex, MessageLevel.Error, s_jsonPointers[nameof(this.PatternProperties)] + new JsonPointer(x));

        if (this.AllOfSchemas is not null && this.AllOfSchemas.Count == 0)
            this.Log(Literals.ExpectedNonEmptyArray, MessageLevel.Error, s_jsonPointers[nameof(this.AllOfSchemas)]);

        if (this.AnyOfSchemas is not null && this.AnyOfSchemas.Count == 0)
            this.Log(Literals.ExpectedNonEmptyArray, MessageLevel.Error, s_jsonPointers[nameof(this.AnyOfSchemas)]);

        if (this.OneOfSchemas is not null && this.OneOfSchemas.Count == 0)
            this.Log(Literals.ExpectedNonEmptyArray, MessageLevel.Error, s_jsonPointers[nameof(this.OneOfSchemas)]);

        bool hasIf = this.ConditionIfSchema is not null;
        bool hasThen = this.ConditionThenSchema is not null;
        bool hasElse = this.ConditionElseSchema is not null;

        if (!hasIf && (hasThen || hasElse))
            this.Log(
                "\"Conditional then/else\" schema will be ignored: \"conditional if\" schema is not present.",
                MessageLevel.Warning);

        if (hasIf && !hasThen && !hasElse)
            this.Log(
                "\"Conditional if\" schema will be ignored: \"conditional then/else\" schema is not present.",
                MessageLevel.Warning);
    }
}
