using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Ropufu.Json;

public sealed class BasicSchemaNoexceptConverter<TSchema>
    : NoexceptJsonConverter<TSchema>
    where TSchema : BasicSchema<TSchema>, new()
{
    public override Utf8JsonParser<TSchema> MakeParser(NullabilityAwareType<TSchema> typeToConvert)
        => typeToConvert.IsNotNull
        ? BasicSchemaNoexceptConverter<TSchema>.TryGetNotNull
        : BasicSchemaNoexceptConverter<TSchema>.TryGetMaybeNull;

    public static bool TryGetMaybeNull(ref Utf8JsonReader json, out TSchema? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.True:
                value = BasicSchema<TSchema>.TrivialTrue;
                return true;
            case JsonTokenType.False:
                value = BasicSchema<TSchema>.TrivialFalse;
                return true;
            case JsonTokenType.StartObject:
                return JsonObjectNoexceptConverter<TSchema>.TryGetNotNull(ref json, out value);
            default:
                value = null;
                return false;
        } // switch (...)
    }

    public static bool TryGetNotNull(ref Utf8JsonReader json, [MaybeNullWhen(returnValue: false)] out TSchema value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.True:
                value = BasicSchema<TSchema>.TrivialTrue;
                return true;
            case JsonTokenType.False:
                value = BasicSchema<TSchema>.TrivialFalse;
                return true;
            case JsonTokenType.StartObject:
                return JsonObjectNoexceptConverter<TSchema>.TryGetNotNull(ref json, out value);
            default:
                value = null;
                return false;
        } // switch (...)
    }
}

public sealed class BasicSchemaNoexceptConverterFactory
    : NoexceptJsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        Type basicSchemaType = typeof(BasicSchema<>).MakeGenericType(typeToConvert);
        return typeToConvert.BaseType == basicSchemaType;
    }

    public override NoexceptJsonConverter CreateConverter(NullabilityAwareType typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!this.CanConvert(typeToConvert.Type))
            throw new NotSupportedException("BasicSchemaConverterFactory only supports types directly inherited from BasicSchema`[T].");

        Type converterType = typeof(BasicSchemaNoexceptConverter<>).MakeGenericType(typeToConvert.Type);
        return (NoexceptJsonConverter)Activator.CreateInstance(converterType)!;
    }
}
