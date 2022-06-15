using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Ropufu.Json.Tests;

public class BasicSchemaTest
{
    private static readonly NullabilityInfoContext s_context = new();

    private static bool IsMatch(Schema schema, string testJson)
        => schema.IsMatch(BasicSchemaTest.AsJsonElement(testJson));

    private static JsonElement AsJsonElement(string json)
        => JsonDocument.Parse(json).RootElement;

    private static string? GetJsonName(SimpleType that)
    {
        FieldInfo? info = typeof(SimpleType).GetField(that.ToString(), BindingFlags.Public | BindingFlags.Static);
        if (info is null)
            return null;

        JsonPropertyNameAttribute? nameAttribute = info.GetCustomAttribute<JsonPropertyNameAttribute>(false);
        JsonIgnoreAttribute? ignoreAttribute = info.GetCustomAttribute<JsonIgnoreAttribute>(false);

        if (nameAttribute is null)
            return null;

        if ((ignoreAttribute is not null) && (ignoreAttribute.Condition == JsonIgnoreCondition.Always))
            return null;

        return nameAttribute.Name;
    }

    private static bool TryDeserialize<T>(string json, [MaybeNullWhen(returnValue: false)] out T value)
    {
        value = default;

        NullabilityAwareType<T> typeToConvert = NullabilityAwareType<T>.Unknown(s_context);

        if (!NoexceptJson.TryMakeParser<T>(typeToConvert, out Utf8JsonParser<T>? parser))
            return false;

        byte[] utf8Bytes = Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new(utf8Bytes);
        reader.Read();
        return parser(ref reader, out value);
    }

    [Fact]
    public void BooleanTrueSchema()
    {
        Assert.True(BasicSchemaTest.TryDeserialize("true", out Schema? schema));
        Assert.True(schema!.IsTrivialTrue);
        Assert.False(schema!.IsTrivialFalse);
        Assert.Equal(0, schema!.ErrorMessages.Count);
    }

    [Fact]
    public void BooleanFalseSchema()
    {
        Assert.True(BasicSchemaTest.TryDeserialize("false", out Schema? schema));
        Assert.False(schema!.IsTrivialTrue);
        Assert.True(schema!.IsTrivialFalse);
        Assert.Equal(0, schema!.ErrorMessages.Count);
    }

    [Fact]
    public void EmptyObjectSchema()
    {
        Assert.True(BasicSchemaTest.TryDeserialize("{}", out Schema? schema));
        Assert.False(schema!.IsTrivialTrue);
        Assert.False(schema!.IsTrivialFalse);
        Assert.Equal(0, schema!.ErrorMessages.Count);
    }

    [Fact]
    public void SimpleTypeSchema()
    {
        foreach (SimpleType simple in Enum.GetValues<SimpleType>())
        {
            if (simple == SimpleType.Missing)
                continue;

            string? name = BasicSchemaTest.GetJsonName(simple);

            if (name is null)
                continue;

            string json = $"{{\"type\":\"{name}\"}}";
            Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
            Assert.Equal(simple, schema!.Type);
            Assert.Equal(0, schema!.ErrorMessages.Count);
        } // foreach (...)
    }

    [Fact]
    public void SimpleTypeOrBooleanSchema()
    {
        foreach (SimpleType simple in Enum.GetValues<SimpleType>())
        {
            if ((simple == SimpleType.Missing) || (simple == SimpleType.Boolean))
                continue;

            string? name = BasicSchemaTest.GetJsonName(simple);

            if (name is null)
                continue;

            SimpleType compound = SimpleType.Boolean | simple;

            string json = $"{{\"type\":[\"boolean\",\"{name}\"]}}";
            Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
            Assert.Equal(compound, schema!.Type);
            Assert.Equal(0, schema!.ErrorMessages.Count);
        } // foreach (...)
    }

    [Fact]
    public void InvalidPatternLogs()
    {
        string json = "{\"patternProperties\":{\"invalid (( pattern\":true}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.True(schema!.Has(ErrorLevel.Error));
    }

    [Fact]
    public void NoPermissibleValues()
    {
        string json = "{\"enum\":[]}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);

        Assert.False(BasicSchemaTest.IsMatch(schema!, "17"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "2.9"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "\"\""));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "null"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "false"));
    }

    [Fact]
    public void NumericMinimumValidation()
    {
        string json = "{\"minimum\":17}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);

        Assert.False(BasicSchemaTest.IsMatch(schema!, "16"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "16.9"));

        Assert.True(BasicSchemaTest.IsMatch(schema!, "17"));
        Assert.True(BasicSchemaTest.IsMatch(schema!, "17.0"));

        Assert.True(BasicSchemaTest.IsMatch(schema!, "18"));
        Assert.True(BasicSchemaTest.IsMatch(schema!, "18.9"));
    }

    [Fact]
    public void NumericExclusiveMaximumValidation()
    {
        string json = "{\"exclusiveMaximum\":29}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);

        Assert.True(BasicSchemaTest.IsMatch(schema!, "28"));
        Assert.True(BasicSchemaTest.IsMatch(schema!, "28.9"));

        Assert.False(BasicSchemaTest.IsMatch(schema!, "29"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "29.0"));

        Assert.False(BasicSchemaTest.IsMatch(schema!, "30"));
        Assert.False(BasicSchemaTest.IsMatch(schema!, "30.9"));
    }

    [Fact]
    public void TwoDefs()
    {
        string json = "{\"$defs\":{\"foo\":false,\"bar\":null}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.Equal(2, schema!.Definitions.Count);
    }

    [Fact]
    public void TwoProperties()
    {
        string json = "{\"properties\":{\"foo\":false,\"bar\":null}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.Equal(2, schema!.Properties.Count);
    }

    [Fact]
    public void SimpleReference()
    {
        string json = "{\"$defs\":{\"foo\":false},\"items\":{\"$ref\":\"#/$defs/foo\"}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.True(schema!.TryResolveLocalReferences(out _));
        Assert.True(object.ReferenceEquals(
            schema!.ArrayItemsSchema.ResolvedReference,
            schema!.Definitions["foo"]
            ));
    }

    [Fact]
    public void AnchoredReference()
    {
        string json = "{\"$defs\":{\"foo\":{\"$anchor\":\"bar\"}},\"items\":{\"$ref\":\"#bar\"}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.True(schema!.TryResolveLocalReferences(out _));
        Assert.True(object.ReferenceEquals(
            schema!.ArrayItemsSchema.ResolvedReference,
            schema!.Definitions["foo"]
            ));
    }

    [Fact]
    public void DeepCircularReference()
    {
        string json = @"
{
    ""$defs"": {
        ""a"": {
            ""$defs"": {
                ""b"": {
                    ""$defs"": {
                        ""c"": {
                            ""$ref"": ""#/$defs/a/$defs/b/$defs/c""
                        }
                    }
                }
            }
        }
    }
}
";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.False(schema!.TryResolveLocalReferences(out _));
    }

    [Fact]
    public void CircularReferenceZero()
    {
        string json = "{\"$defs\":{\"foo\":{\"$ref\":\"#/$defs/foo\"}}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.False(schema!.TryResolveLocalReferences(out _));
    }

    [Fact]
    public void CircularReferenceOne()
    {
        string json = "{\"$defs\":{\"foo\":{\"$ref\":\"#/$defs/bar\"},\"bar\":{\"$ref\":\"#/$defs/foo\"}}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.False(schema!.TryResolveLocalReferences(out _));
    }

    [Fact]
    public void CircularReferenceTwo()
    {
        string json = "{\"$defs\":{\"foo\":{\"$ref\":\"#/$defs/bar\"},\"bar\":{\"$ref\":\"#/$defs/baz\"},\"baz\":{\"$ref\":\"#/$defs/foo\"}}}";
        Assert.True(BasicSchemaTest.TryDeserialize(json, out Schema? schema));
        Assert.Equal(0, schema!.ErrorMessages.Count);
        Assert.False(schema!.TryResolveLocalReferences(out _));
    }
}
