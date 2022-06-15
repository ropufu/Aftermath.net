using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    [JsonInclude]
    [JsonPropertyName("type")]
    public SimpleType Type { get; private set; } = SimpleType.Missing;

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
    public int? MaxLength { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minLength")]
    public int MinLength { get; private set; } = 0;

    [JsonInclude]
    [JsonPropertyName("pattern")]
    public Regex Pattern { get; private set; } = new(string.Empty);

    [JsonInclude]
    [JsonPropertyName("maxItems")]
    public int? MaxItems { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minItems")]
    public int MinItems { get; private set; } = 0;

    [JsonInclude]
    [JsonPropertyName("uniqueItems")]
    public bool DoRequireDistinctItems { get; private set; } = false;

    [JsonInclude]
    [JsonPropertyName("maxContains")]
    public int? ArrayMaxContains { get; private set; }

    [JsonInclude]
    [JsonPropertyName("minContains")]
    public int ArrayMinContains { get; private set; } = 1;

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
                this.LogError(Literals.ExpectedFinite, s_jsonPointers[nameof(this.MultipleOf)]);
            else if (this.MultipleOf.Value <= 0)
                this.LogError(Literals.ExpectedPositive, s_jsonPointers[nameof(this.MultipleOf)]);
        } // if (...)

        this.ValidateLowerBound();
        this.ValidateUpperBound();
        this.ValidateInterval();

        if (this.MaxLength.HasValue && this.MaxLength.Value < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MaxLength)]);

        if (this.MinLength < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MinLength)]);

        if (this.MaxLength.HasValue && this.MaxLength.Value < this.MinLength)
            this.LogError("Maximum length cannot exceed minimum length.");

        if (this.MaxItems.HasValue && this.MaxItems.Value < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MaxItems)]);

        if (this.MinItems < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MinItems)]);

        if (this.MaxItems.HasValue && this.MaxItems.Value < this.MinItems)
            this.LogError("Maximum number of items cannot exceed minimum number of items.");

        if (this.ArrayMaxContains.HasValue && this.ArrayMaxContains.Value < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.ArrayMaxContains)]);

        if (this.ArrayMinContains < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.ArrayMinContains)]);

        if (this.ArrayMaxContains.HasValue && this.ArrayMaxContains.Value < this.ArrayMinContains)
            this.LogError("Maximum number of contains cannot exceed minimum number of contains.");

        if (this.MaxProperties.HasValue && this.MaxProperties.Value < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MaxProperties)]);

        if (this.MinProperties < 0)
            this.LogError(Literals.ExpectedNonNegative, s_jsonPointers[nameof(this.MinProperties)]);

        if (this.MaxProperties.HasValue && this.MaxProperties.Value < this.MinProperties)
            this.LogError("Maximum number of properties cannot exceed minimum number of properties.");

        if (!this.RequiredPropertyNames.IsDistinct())
            this.LogError(Literals.ExpectedDistinctItems, s_jsonPointers[nameof(this.RequiredPropertyNames)]);

        foreach (KeyValuePair<string, ImmutableList<string>> x in this.PropertyDependentRequiredPropertyNames)
            if (!x.Value.IsDistinct())
                this.LogError(Literals.ExpectedDistinctItems, s_jsonPointers[nameof(this.PropertyDependentRequiredPropertyNames)].Append(x.Key));
    }

    private void ValidateLowerBound()
    {
        if (!this.Minimum.IsFiniteOrNull())
            this.LogError(Literals.ExpectedFinite, s_jsonPointers[nameof(this.Minimum)]);

        if (!this.ExclusiveMinimum.IsFiniteOrNull())
            this.LogError(Literals.ExpectedFinite, s_jsonPointers[nameof(this.ExclusiveMinimum)]);

        if (!this.Minimum.HasValue)
            return;

        if (!this.ExclusiveMinimum.HasValue)
            return;

        if (this.Minimum.Value > this.ExclusiveMinimum.Value)
            this.LogWarning("Exclusive minimum will be ignored as minimum is more restrictive.");
        else
            this.LogWarning("Minimum will be ignored as exclusive minimum is more restrictive.");
    }

    private void ValidateUpperBound()
    {
        if (!this.Maximum.IsFiniteOrNull())
            this.LogError(Literals.ExpectedFinite, s_jsonPointers[nameof(this.Maximum)]);

        if (!this.ExclusiveMaximum.IsFiniteOrNull())
            this.LogError(Literals.ExpectedFinite, s_jsonPointers[nameof(this.ExclusiveMaximum)]);

        if (!this.Maximum.HasValue)
            return;

        if (!this.ExclusiveMaximum.HasValue)
            return;

        if (this.Maximum.Value < this.ExclusiveMaximum.Value)
            this.LogWarning("Exclusive maximum will be ignored as maximum is more restrictive.");
        else
            this.LogWarning("Maximum will be ignored as exclusive maximum is more restrictive.");
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
            this.LogError("Lower bound cannot exceed upper bound.");
            return;
        } // if (...)

        // Now that we know that both are equal...
        if (isLowerExclusive || isUpperExclusive)
            this.LogError("Bounds result in no acceptable values.");
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
        if (value.Length < this.MinLength)
            return false;

        if (this.MaxLength.HasValue && value.Length > this.MaxLength.Value)
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

        if (n < this.MinItems)
            return false;

        if (this.MaxItems.HasValue && n > this.MaxItems.Value)
            return false;

        if (this.DoRequireDistinctItems)
        {
            for (int i = 0; i < n; ++i)
            {
                JsonElement x = list[i];
                for (int j = i + 1; j < n; ++j)
                    if (x.IsEquivalent(list[j]))
                        return false;
            } // for (...)
        } // if (...)

        int offset = 0;
        foreach (TSchema schema in this.ArrayPrefixItemSchemas)
        {
            if (!schema.IsMatch(list[offset]))
                return false;
            ++offset;
        } // foreach (...)

        for (int i = offset; i < n; ++i)
            if (!this.ArrayItemsSchema.IsMatch(list[i]))
                return false;

        if (this.ArrayContainsSchema is not null)
        {
            int countContains = 0;
            for (int i = 0; i < n; ++i)
                if (this.ArrayContainsSchema.IsMatch(list[i]))
                    ++countContains;

            if (countContains < this.ArrayMinContains)
                return false;

            if (this.ArrayMaxContains.HasValue && countContains > this.ArrayMaxContains.Value)
                return false;
        } // if (...)

        return true;
    }
}
