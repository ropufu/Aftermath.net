using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

/// <summary>
/// Provides base for non-throwing JSON schemas. Rather than rely on
/// <see cref="JsonException"/> when parsing fails, keeps track of errors
/// by means of <see cref="VerboseJson"/> functionality.
/// </summary>
/// <remarks>
/// Properties not marked with [<see cref="JsonPropertyNameAttribute"/>] are ignored.
/// </remarks>
/// <typeparam name="TSchema">Implementation of BasicSchema.</typeparam>
[NoexceptJsonConverter(typeof(BasicSchemaNoexceptConverterFactory))]
public abstract partial class BasicSchema<TSchema>
    : VerboseJson, IResponsiveNoexceptJson
    where TSchema : BasicSchema<TSchema>, new()
{
    private bool _isTrivialTrue = false;
    private bool _isTrivialFalse = false;

    // Schemas immediately owned by this schema through properties.
    private ImmutableDictionary<string, TSchema> _immediateSchemas;
    // Indicates if this schema or any of its descendant schemas is a local static reference.
    private bool _doesOwnLocalStaticReferences;
    // Indicates if this schema or any of its descendant schemas is a local dynamic reference.
    private bool _doesOwnLocalDynamicReferences;
    // Indicates if this schema or any of its descendant schemas is an external reference.
    private bool _doesOwnExternalReferences;
    // Indicates if this schema or any of its descendant schemas is an unresolved local reference (either static or dynamic).
    private bool _doesOwnUnresolvedLocalReferences;

    [MemberNotNull(
        nameof(BasicSchema<TSchema>._immediateSchemas),
        nameof(BasicSchema<TSchema>._doesOwnLocalStaticReferences),
        nameof(BasicSchema<TSchema>._doesOwnLocalDynamicReferences))]
    private void Initialize()
    {
        this.Clear();

        _immediateSchemas = new(BasicSchema<TSchema>.InitializeImmediateSchemas((TSchema)this));
        _doesOwnLocalStaticReferences = false;
        _doesOwnLocalDynamicReferences = false;
        _doesOwnExternalReferences = false;
        _doesOwnUnresolvedLocalReferences = false;

        this.ResolvedReference = null;

        // Base case.
        if (this.IsLocalStaticReference)
            _doesOwnLocalStaticReferences = true;

        if (this.IsLocalDynamicReference)
            _doesOwnLocalDynamicReferences = true;

        if (this.IsExternalReference)
            _doesOwnExternalReferences = true;

        // Note: initialization on children has already happened.
        foreach (TSchema x in _immediateSchemas.Values)
        {
            _doesOwnLocalStaticReferences |= x._doesOwnLocalStaticReferences;
            _doesOwnLocalDynamicReferences |= x._doesOwnLocalDynamicReferences;
            _doesOwnExternalReferences |= x._doesOwnExternalReferences;
        } // foreach (...)

        if (_doesOwnLocalStaticReferences || _doesOwnLocalDynamicReferences)
            _doesOwnUnresolvedLocalReferences = true;
    }

    public BasicSchema()
    {
        this.Initialize();
    }

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

    [JsonIgnore]
    public bool IsTrivial => _isTrivialTrue || _isTrivialFalse;

    [JsonIgnore]
    public bool IsTrivialTrue => _isTrivialTrue;

    [JsonIgnore]
    public bool IsTrivialFalse => _isTrivialFalse;

    [JsonIgnore]
    [MemberNotNullWhen(returnValue: true, nameof(BasicSchema<TSchema>.StaticReference))]
    public bool IsLocalStaticReference
        => this.StaticReference is not null && this.StaticReference.StartsWith('#');

    [JsonIgnore]
    [MemberNotNullWhen(returnValue: true, nameof(BasicSchema<TSchema>.DynamicReference))]
    public bool IsLocalDynamicReference
        => this.DynamicReference is not null && this.DynamicReference.StartsWith('#');

    [JsonIgnore]
    public bool IsExternalReference
        => (this.StaticReference is not null && !this.StaticReference.StartsWith('#'))
        || (this.DynamicReference is not null && !this.DynamicReference.StartsWith('#'));

    protected ImmutableDictionary<string, TSchema> GetImmediateSchemas()
        => _immediateSchemas;

    /// <summary>
    /// Schemas owned by this schema or any of its descendants.
    /// </summary>
    protected Dictionary<string, TSchema> MapChildSchemas()
    {
        Dictionary<string, TSchema> schemaMap = new();

        foreach (KeyValuePair<string, TSchema> x in _immediateSchemas)
            schemaMap.Add(x.Key, x.Value);

        foreach (KeyValuePair<string, TSchema> x in _immediateSchemas)
            foreach (KeyValuePair<string, TSchema> y in x.Value.MapChildSchemas())
                schemaMap.Add(x.Key + y.Key, y.Value);

        return schemaMap;
    }

    /// <summary>
    /// After a successfull call to TryResolveLocalReferences on the root schema, contains referenced schema.
    /// </summary>
    [JsonIgnore]
    public TSchema? ResolvedReference { get; set; }

    /// <summary>
    /// Tries to resolve local references (references starting with '#') assuming this schema is the root value of a JSON document. 
    /// </summary>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Cannot resolve references on malformed schemas.</exception>
    public bool TryResolveLocalReferences(out JsonLogger logger)
    {
        logger = new();
        TSchema that = (TSchema)this;

        if (this.Has(MessageLevel.Error))
            throw new NotSupportedException("Cannot resolve references on malformed schemas.");

        if (!_doesOwnUnresolvedLocalReferences)
            return true;

        // All desccendant schemas along with this schema, mapped by JSON Pointer.
        Dictionary<string, TSchema> childrenAndI = that.MapChildSchemas();
        childrenAndI.Add("", that);

        BasicSchema<TSchema>.MapLocalAnchors(
            childrenAndI,
            out Dictionary<string, string> anchorPointers,
            out List<TSchema> unresolvedSchemas,
            logger);

        BasicSchema<TSchema>.ResolveLocalReferences(
            childrenAndI,
            anchorPointers,
            unresolvedSchemas,
            logger);

        BasicSchema<TSchema>.CheckCircularReferences(childrenAndI, logger);

        // Revert to the original state if resolution failed.
        if (logger.Has(MessageLevel.Error))
        {
            foreach (TSchema x in unresolvedSchemas)
            {
                x.ResolvedReference = null;
                x._doesOwnUnresolvedLocalReferences = true;
            } // foreach (...)

            return false;
        } // if (...)
        else
        {
            foreach (TSchema x in unresolvedSchemas)
                x._doesOwnUnresolvedLocalReferences = false;

            return true;
        } // else
    }

    /// <exception cref="NotSupportedException">Cannot validate against malformed schema.</exception>
    /// <exception cref="NotSupportedException">Cannot validate against external references.</exception>
    /// <exception cref="InvalidOperationException">Schema references has not been resolved. Successfull call to TryResolveLocalReferences required.</exception>
    public bool IsMatch(JsonElement element)
        => this.IsMatch(ref element);

    /// <exception cref="NotSupportedException">Cannot validate against malformed schema.</exception>
    /// <exception cref="NotSupportedException">Cannot validate against external references.</exception>
    /// <exception cref="InvalidOperationException">Schema references have not been resolved. Successfull call to TryResolveLocalReferences required.</exception>
    public bool IsMatch(ref JsonElement element)
    {
        if (this.Has(MessageLevel.Error))
            throw new NotSupportedException("Cannot validate against malformed schema.");

        if (_doesOwnExternalReferences)
            throw new NotSupportedException("Cannot validate against external references.");

        if (_doesOwnUnresolvedLocalReferences)
            throw new InvalidOperationException("Schema references have not been resolved. Successfull call to TryResolveLocalReferences required.");

        if (_isTrivialTrue)
            return true;

        if (_isTrivialFalse)
            return false;

        if (this.ResolvedReference is not null && !this.ResolvedReference.IsMatch(ref element))
            return false;

        if (this.ConditionIfSchema is not null)
        {
            bool doesMatchIf = this.ConditionIfSchema.IsMatch(ref element);

            if (doesMatchIf && this.ConditionThenSchema is not null)
                if (!this.ConditionThenSchema.IsMatch(ref element))
                    return false;

            if (!doesMatchIf && this.ConditionElseSchema is not null)
                if (!this.ConditionElseSchema.IsMatch(ref element))
                    return false;
        } // if (...)

        if (this.AllOfSchemas is not null)
        {
            foreach (TSchema x in this.AllOfSchemas)
                if (!x.IsMatch(ref element))
                    return false;
        } // if (...)

        if (this.AnyOfSchemas is not null)
        {
            bool isGood = false;

            foreach (TSchema x in this.AnyOfSchemas)
                if (x.IsMatch(ref element))
                    isGood = true;

            if (!isGood)
                return false;
        } // if (...)

        if (this.OneOfSchemas is not null)
        {
            int countGood = 0;

            foreach (TSchema x in this.OneOfSchemas)
                if (x.IsMatch(ref element))
                    ++countGood;

            if (countGood != 1)
                return false;
        } // if (...)

        if (this.ConditionNotSchema is not null && this.ConditionNotSchema.IsMatch(ref element))
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

    protected abstract void OnDeserializedOverride();

    public void OnDeserialized()
    {
        this.Initialize();

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
                this.Log(
                    $"Constant value \"{constantValue.GetRawText()}\" does not pass schema validation.",
                    MessageLevel.Error,
                    s_jsonPointers[nameof(this.ConstantValue)]);

        this.ConstantValue = constantValue;

        // Validate permissible values against incomplete (no PV) schema.
        if (permissibleValues is not null)
            foreach (JsonElement x in permissibleValues)
                if (!this.IsMatch(x))
                    this.Log(
                        $"Permissible value \"{x.GetRawText()}\" does not pass schema validation.",
                        MessageLevel.Error,
                        s_jsonPointers[nameof(this.PermissibleValues)]);

        this.PermissibleValues = permissibleValues;

        if (this.ConstantValue.ValueKind != JsonValueKind.Undefined && this.PermissibleValues is not null)
        {
            // Permissible values have already been validated against CV.
            if (this.PermissibleValues.Count == 0)
                this.Log(
                    "Constant value is not present in permissible value list.",
                    MessageLevel.Error);
        } // if (...)

        // Parse examples against complete schema.
        foreach (JsonElement x in this.Examples)
            if (!this.IsMatch(x))
                this.Log(
                    $"Example \"{x.GetRawText()}\" does not pass schema validation.",
                    MessageLevel.Error,
                    s_jsonPointers[nameof(this.PermissibleValues)]);

        if (this.DeprecatedDefinitions.ValueKind != JsonValueKind.Undefined)
            this.Log(
                "\"definitions\" has been replaced by \"$defs\".",
                MessageLevel.Warning,
                s_jsonPointers[nameof(this.DeprecatedDefinitions)]);

        if (this.DeprecatedDependencies.ValueKind != JsonValueKind.Undefined)
            this.Log(
                "\"dependencies\" has been split and replaced by \"dependentSchemas\" and \"dependentRequired\" in order to serve their differing semantics.",
                MessageLevel.Warning,
                s_jsonPointers[nameof(this.DeprecatedDependencies)]);

        if (this.DeprecatedRecursiveAnchor.ValueKind != JsonValueKind.Undefined)
            this.Log(
                "\"$recursiveAnchor\" has been replaced by \"$dynamicAnchor\".",
                MessageLevel.Warning,
                s_jsonPointers[nameof(this.DeprecatedRecursiveAnchor)]);

        if (this.DeprecatedRecursiveReference.ValueKind != JsonValueKind.Undefined)
            this.Log(
                "\"$recursiveRef\" has been replaced by \"$dynamicRef\".",
                MessageLevel.Warning,
                s_jsonPointers[nameof(this.DeprecatedRecursiveReference)]);

        this.OnDeserializedOverride();

        // Aggregate console messages. Note that immediate children have already performed
        // aggregation and TSchema has logged its own messages.
        foreach (KeyValuePair<string, TSchema> x in _immediateSchemas)
            this.LogRange(x.Value.Messages, JsonPointer.Parse(x.Key));
    }

    public void OnDeserializing()
    {
    }

    public void OnParsingFailure(string jsonPropertyName, ref Utf8JsonReader propertyJson)
    {
        this.Log(
            $"Parsing property \"{jsonPropertyName}\" failed. Encountered: [{propertyJson.TokenType}].",
            MessageLevel.Error);
    }

    public void OnRequiredPropertyMissing(string jsonPropertyName)
    {
        this.Log(
            $"Required property \"{jsonPropertyName}\" missing.",
            MessageLevel.Error);
    }
}
