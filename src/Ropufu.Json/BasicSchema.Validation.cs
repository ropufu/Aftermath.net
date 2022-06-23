using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("type")]
    public SimpleType Type { get; private set; } = SimpleType.Missing;

    [JsonIgnore]
    public int TypeCount
    {
        get
        {
            int count = 0;

            foreach (SimpleType x in Enum.GetValues<SimpleType>())
                if ((this.Type & x) != SimpleType.Missing)
                    ++count;

            return count;
        }
    }

    [JsonInclude]
    [JsonPropertyName("const")]
    public JsonElement ConstantValue { get; private set; }

    [JsonInclude]
    [JsonPropertyName("enum")]
    public ImmutableList<JsonElement>? PermissibleValues { get; private set; }

    [JsonInclude]
    [JsonPropertyName("multipleOf")]
    public double? MultipleOf { get; private set; }

    [JsonInclude]
    [JsonPropertyName("maximum")]
    public double? Maximum { get; private set; }

    [JsonInclude]
    [JsonPropertyName("exclusiveMaximum")]
    public double? ExclusiveMaximum { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minimum")]
    public double? Minimum { get; private set; }

    [JsonInclude]
    [JsonPropertyName("exclusiveMinimum")]
    public double? ExclusiveMinimum { get; private set; }

    [JsonInclude]
    [JsonPropertyName("maxLength")]
    public int? MaxStringLength { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minLength")]
    public int MinStringLength { get; private set; } = 0;

    [JsonInclude]
    [JsonPropertyName("pattern")]
    public Regex Pattern { get; private set; } = new(string.Empty);

    [JsonInclude]
    [JsonPropertyName("maxItems")]
    public int? MaxArrayItems { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minItems")]
    public int MinArrayItems { get; private set; } = 0;

    [JsonInclude]
    [JsonPropertyName("uniqueItems")]
    public bool DoRequireDistinctArrayItems { get; private set; } = false;

    [JsonInclude]
    [JsonPropertyName("maxContains")]
    public int? ArrayMaxArrayContains { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minContains")]
    public int ArrayMinArrayContains { get; private set; } = 1;

    [JsonInclude]
    [JsonPropertyName("maxProperties")]
    public int? MaxProperties { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minProperties")]
    public int MinProperties { get; private set; } = 0;

    [JsonInclude]
    [JsonPropertyName("required")]
    public ImmutableList<string> RequiredPropertyNames { get; private set; } = new();

    [JsonInclude]
    [JsonPropertyName("dependentRequired")]
    public ImmutableDictionary<string, ImmutableList<string>> PropertyDependentRequiredPropertyNames { get; private set; } = new();

    private void InitializeValidationBlock()
    {
        if (this.MultipleOf.HasValue)
        {
            if (!this.MultipleOf.Value.IsFinite())
                this.Log(Literals.ExpectedFinite, MessageLevel.Error, s_jsonPointers[nameof(this.MultipleOf)]);
            else if (this.MultipleOf.Value <= 0)
                this.Log(Literals.ExpectedPositive, MessageLevel.Error, s_jsonPointers[nameof(this.MultipleOf)]);
        } // if (...)

        this.ValidateLowerBound();
        this.ValidateUpperBound();
        this.ValidateInterval();

        if (this.MaxStringLength.HasValue && this.MaxStringLength.Value < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MaxStringLength)]);

        if (this.MinStringLength < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MinStringLength)]);

        if (this.MaxStringLength.HasValue && this.MaxStringLength.Value < this.MinStringLength)
            this.Log(
                "Maximum length cannot exceed minimum length.",
                MessageLevel.Error);

        if (this.MaxArrayItems.HasValue && this.MaxArrayItems.Value < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MaxArrayItems)]);

        if (this.MinArrayItems < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MinArrayItems)]);

        if (this.MaxArrayItems.HasValue && this.MaxArrayItems.Value < this.MinArrayItems)
            this.Log(
                "Maximum number of items cannot exceed minimum number of items.",
                MessageLevel.Error);

        if (this.ArrayMaxArrayContains.HasValue)
        {
            if (this.ArrayMaxArrayContains.Value < 0)
                this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.ArrayMaxArrayContains)]);

            if (this.ArrayContainsSchema is null)
                this.Log(
                    "Maximum number of \"contains\" will be ignored: \"contains\" schema is not present.",
                    MessageLevel.Warning);
        } // if (...)

        if (this.ArrayMinArrayContains < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.ArrayMinArrayContains)]);

        // ArrayMinArrayContains defaults to 1.
        if (this.ArrayMinArrayContains > 1 && this.ArrayContainsSchema is null)
            this.Log(
                "Minimum number of \"contains\" will be ignored: \"contains\" schema is not present.",
                MessageLevel.Warning);

        if (this.ArrayMaxArrayContains.HasValue && this.ArrayMaxArrayContains.Value < this.ArrayMinArrayContains)
            this.Log(
                "Maximum number of contains cannot exceed minimum number of contains.",
                MessageLevel.Error);

        if (this.MaxProperties.HasValue && this.MaxProperties.Value < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MaxProperties)]);

        if (this.MinProperties < 0)
            this.Log(Literals.ExpectedNonNegative, MessageLevel.Error, s_jsonPointers[nameof(this.MinProperties)]);

        if (this.MaxProperties.HasValue && this.MaxProperties.Value < this.MinProperties)
            this.Log(
                "Maximum number of properties cannot exceed minimum number of properties.",
                MessageLevel.Error);

        if (!this.RequiredPropertyNames.IsDistinct())
            this.Log(Literals.ExpectedDistinctItems, MessageLevel.Error, s_jsonPointers[nameof(this.RequiredPropertyNames)]);

        foreach (KeyValuePair<string, ImmutableList<string>> x in this.PropertyDependentRequiredPropertyNames)
            if (!x.Value.IsDistinct())
                this.Log(Literals.ExpectedDistinctItems, MessageLevel.Error, s_jsonPointers[nameof(this.PropertyDependentRequiredPropertyNames)] + new JsonPointer(x.Key));
    }

    private void ValidateLowerBound()
    {
        if (!this.Minimum.IsFiniteOrNull())
            this.Log(Literals.ExpectedFinite, MessageLevel.Error, s_jsonPointers[nameof(this.Minimum)]);

        if (!this.ExclusiveMinimum.IsFiniteOrNull())
            this.Log(Literals.ExpectedFinite, MessageLevel.Error, s_jsonPointers[nameof(this.ExclusiveMinimum)]);

        if (!this.Minimum.HasValue)
            return;

        if (!this.ExclusiveMinimum.HasValue)
            return;

        if (this.Minimum.Value > this.ExclusiveMinimum.Value)
            this.Log(
                "Exclusive minimum will be ignored as minimum is more restrictive.",
                MessageLevel.Warning);
        else
            this.Log(
                "Minimum will be ignored as exclusive minimum is more restrictive.",
                MessageLevel.Warning);
    }

    private void ValidateUpperBound()
    {
        if (!this.Maximum.IsFiniteOrNull())
            this.Log(Literals.ExpectedFinite, MessageLevel.Error, s_jsonPointers[nameof(this.Maximum)]);

        if (!this.ExclusiveMaximum.IsFiniteOrNull())
            this.Log(Literals.ExpectedFinite, MessageLevel.Error, s_jsonPointers[nameof(this.ExclusiveMaximum)]);

        if (!this.Maximum.HasValue)
            return;

        if (!this.ExclusiveMaximum.HasValue)
            return;

        if (this.Maximum.Value < this.ExclusiveMaximum.Value)
            this.Log(
                "Exclusive maximum will be ignored as maximum is more restrictive.",
                MessageLevel.Warning);
        else
            this.Log(
                "Maximum will be ignored as exclusive maximum is more restrictive.",
                MessageLevel.Warning);
    }

    private void ValidateInterval()
    {
        if (!this.TryGetLowerBound(out double lowerBound, out bool isLowerExclusive))
            return;

        if (!this.TryGetUpperBound(out double upperBound, out bool isUpperExclusive))
            return;

        if (lowerBound < upperBound)
            return;

        if (lowerBound > upperBound)
        {
            this.Log(
                "Lower bound cannot exceed upper bound.",
                MessageLevel.Error);
            return;
        } // if (...)

        // Now that we know that both are equal...
        if (isLowerExclusive || isUpperExclusive)
            this.Log(
                "Bounds result in no acceptable values.",
                MessageLevel.Error);
    }

    public bool TryGetLowerBound(out double bound, out bool isExclusivee)
    {
        bound = default;
        isExclusivee = false;

        if (this.Minimum.HasValue)
        {
            bound = this.Minimum.Value;

            if (!this.ExclusiveMinimum.HasValue)
                return true;

            if (bound > this.ExclusiveMinimum.Value)
                return true;

            bound = this.ExclusiveMinimum.Value;
            isExclusivee = true;
            return true;
        }
        else if (this.ExclusiveMinimum.HasValue)
        {
            bound = this.ExclusiveMinimum.Value;
            isExclusivee = true;
            return true;
        }
        else
            return false;
    }

    public bool TryGetUpperBound(out double bound, out bool isExclusivee)
    {
        bound = default;
        isExclusivee = false;

        if (this.Maximum.HasValue)
        {
            bound = this.Maximum.Value;

            if (!this.ExclusiveMaximum.HasValue)
                return true;

            if (bound < this.ExclusiveMaximum.Value)
                return true;

            bound = this.ExclusiveMaximum.Value;
            isExclusivee = true;
            return true;
        }
        else if (this.ExclusiveMaximum.HasValue)
        {
            bound = this.ExclusiveMaximum.Value;
            isExclusivee = true;
            return true;
        }
        else
            return false;
    }

    private bool IsPermissible(ref JsonElement value)
    {
        if (this.ConstantValue.ValueKind == JsonValueKind.Undefined)
        {
            if (this.PermissibleValues is null)
                return true;

            foreach (JsonElement x in this.PermissibleValues)
                if (value.IsEquivalent(x))
                    return true;

            return false;
        } // if (...)

        return value.IsEquivalent(this.ConstantValue);
    }

    private bool TryValidate(long value)
    {
        if (this.MultipleOf.HasValue)
            if (value % this.MultipleOf.Value != 0)
                return false;

        if (this.TryGetLowerBound(out double min, out bool isMinExclusive))
        {
            if (value < min)
                return false;
            if (isMinExclusive && value == min)
                return false;
        } // if (...)

        if (this.TryGetUpperBound(out double max, out bool isMaxExclusive))
        {
            if (value > max)
                return false;
            if (isMaxExclusive && value == max)
                return false;
        } // if (...)

        return true;
    }

    private bool TryValidate(double value)
    {
        if (this.MultipleOf.HasValue)
            if (value % this.MultipleOf.Value != 0)
                return false;

        if (this.TryGetLowerBound(out double min, out bool isMinExclusive))
        {
            if (value < min)
                return false;
            if (isMinExclusive && value == min)
                return false;
        } // if (...)

        if (this.TryGetUpperBound(out double max, out bool isMaxExclusive))
        {
            if (value > max)
                return false;
            if (isMaxExclusive && value == max)
                return false;
        } // if (...)

        return true;
    }

    private bool TryValidate(string value)
    {
        if (value.Length < this.MinStringLength)
            return false;

        if (this.MaxStringLength.HasValue && value.Length > this.MaxStringLength.Value)
            return false;

        if (!this.Pattern.IsMatch(value))
            return false;

        return true;
    }

    private bool TryValidateObject(ref JsonElement element)
    {
        // @todo Implement object validation.
        throw new NotImplementedException();
    }

    private bool TryValidateArray(ref JsonElement element)
    {
        List<JsonElement> list = element.GetArrayAsList();

        int n = list.Count;

        if (n < this.MinArrayItems)
            return false;

        if (this.MaxArrayItems.HasValue && n > this.MaxArrayItems.Value)
            return false;

        if (this.DoRequireDistinctArrayItems)
        {
            for (int i = 0; i < n; ++i)
            {
                JsonElement x = list[i];
                for (int j = i + 1; j < n; ++j)
                    if (x.IsEquivalent(list[j]))
                        return false;
            } // for (...)
        } // if (...)

        // Items successfully processed by a schema are considered "evaluated".
        bool[] hasBeenEvaluated = new bool[n];

        int offset = 0;
        foreach (TSchema schema in this.ArrayPrefixItemSchemas)
        {
            if (!schema.IsMatch(list[offset]))
                return false;

            hasBeenEvaluated[offset] = true;
            ++offset;
        } // foreach (...)

        if (this.ArrayItemsSchema is not null)
        {
            for (int i = offset; i < n; ++i)
            {
                if (!this.ArrayItemsSchema.IsMatch(list[i]))
                    return false;

                hasBeenEvaluated[i] = true;
            } // foreach (...)
        } // if (...)

        if (this.ArrayContainsSchema is not null)
        {
            int countContains = 0;

            for (int i = 0; i < n; ++i)
            {
                if (this.ArrayContainsSchema.IsMatch(list[i]))
                {
                    hasBeenEvaluated[i] = true;
                    ++countContains;
                } // if (...)
            } // for (...)

            if (countContains < this.ArrayMinArrayContains)
                return false;

            if (this.ArrayMaxArrayContains.HasValue && countContains > this.ArrayMaxArrayContains.Value)
                return false;
        } // if (...)

        if (this.UnevaluatedArrayItems is not null)
        {
            for (int i = 0; i < n; ++i)
            {
                if (hasBeenEvaluated[i])
                    continue;

                if (!this.UnevaluatedArrayItems.IsMatch(list[i]))
                    return false;
            } // for (...)
        } // if (...)

        return true;
    }
}
