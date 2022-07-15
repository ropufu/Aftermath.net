using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ropufu.Json;

// @todo Implement for structs.
public class JsonObjectNoexceptConverter<T>
    : NoexceptJsonConverter<T>
    where T : class, new()
{
    private abstract class PropertyActivator
    {
        public abstract bool TryParseAndSet(object owner, string propertyName, ref Utf8JsonReader json);
    }

    private sealed class PropertyActivator<TDeclaring, TValue> : PropertyActivator
        where TDeclaring : class
    {
        private readonly Utf8JsonParser<TValue> _parser
            ;
        private readonly Action<TDeclaring, TValue> _setterAction;

        public PropertyActivator(Delegate parser, Delegate setterAction)
        {
            _parser = (Utf8JsonParser<TValue>)parser;
            _setterAction = (Action<TDeclaring, TValue>)setterAction;
        }

        public override bool TryParseAndSet(object owner, string propertyName, ref Utf8JsonReader json)
        {
            if (_parser(ref json, out TValue? x))
            {
                _setterAction((TDeclaring)owner, x!);
                return true;
            } // if (...)

            return false;
        }
    }

    // Set of required JSON property names.
    private static readonly HashSet<string> s_requiredProperties = new();
    // Map [JSON name] -> [Activator].
    private static readonly IReadOnlyDictionary<string, PropertyActivator> s_activators;
    // Map [Property name] -> [JSON name].
    private static readonly IReadOnlyDictionary<string, string> s_names;

    private static PropertyActivator MakeActivator(PropertyInfo propertyInfo, MethodInfo setterInfo, Delegate utf8JsonParser)
    {
        Type declaringType = propertyInfo.DeclaringType!;
        Type valueType = propertyInfo.PropertyType;
        Type setterActionType = typeof(Action<,>).MakeGenericType(declaringType, valueType);
        // PropertyActivator is netsted inside NoexceptJsonCache<T>.
        Type activatorType = typeof(PropertyActivator<,>).MakeGenericType(typeof(T), declaringType, valueType);

        // Action<TOwner, TValue> to set the property value of an instance.
        Delegate setterAction = Delegate.CreateDelegate(setterActionType, setterInfo);

        return (PropertyActivator)Activator.CreateInstance(activatorType, new object?[] {
            utf8JsonParser, setterAction
        })!;
    }

    // @todo Think about circular references.
    static JsonObjectNoexceptConverter()
    {
        const BindingFlags options = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        
        Dictionary<string, PropertyActivator> activators = new();
        Dictionary<string, string> names = new();
        
        s_activators = activators;
        s_names = names;

        NullabilityInfoContext context = new();

        // Traverse all properties in the hierarchy to enable access to private setters.
        for (Type? type = typeof(T); type is not null; type = type.BaseType)
        {
            foreach (PropertyInfo x in type.GetProperties(options))
            {
                NullabilityAwareType propertyType = NullabilityAwareType.FromPropertySetter(x, context);

                JsonPropertyNameAttribute? nameAttribute = x.GetCustomAttribute<JsonPropertyNameAttribute>();
                JsonIgnoreAttribute? ignoreAttribute = x.GetCustomAttribute<JsonIgnoreAttribute>();
                JsonIncludeAttribute? includeAttribute = x.GetCustomAttribute<JsonIncludeAttribute>();
                NoexceptJsonRequiredAttribute? requiredAttribute = x.GetCustomAttribute<NoexceptJsonRequiredAttribute>();
                NoexceptJsonConverterAttribute? specializedConverterAttribute = x.GetCustomAttribute<NoexceptJsonConverterAttribute>();
                
                // Only retrieve private setters for properties marked with [JsonInclude].
                MethodInfo? setterInfo = x.GetSetMethod(nonPublic: includeAttribute is not null);

                // Skip properties that are not marked with [JsonPropertyName].
                if (nameAttribute is null)
                    continue;

                if (!names.TryAdd(x.Name, nameAttribute.Name))
                    throw new NotSupportedException("JSON property names must be distinct.");

                // Skip properties that are marked with [JsonIgnore] or [JsonIgnore(JsonIgnoreCondition.Always)].
                if (ignoreAttribute is not null && ignoreAttribute.Condition == JsonIgnoreCondition.Always)
                    continue;

                // Skip properties that do not have an accessible setter.
                if (setterInfo is null)
                    continue;

                // Keep track of required properties.
                if (requiredAttribute is not null)
                    s_requiredProperties.Add(nameAttribute.Name);

                Delegate? utf8JsonParser = null;

                if (specializedConverterAttribute is not null && specializedConverterAttribute.CanConvert(propertyType.Type))
                    utf8JsonParser = specializedConverterAttribute.MakeUtf8JsonParser(propertyType);
                else if (!NoexceptJson.TryMakeParser(propertyType, out utf8JsonParser))
                    throw new NotSupportedException("Type not recognized. Custom noexcept JSON converter necessary.");

                if (utf8JsonParser is null)
                    throw new NotSupportedException("Custom noexcept JSON converter malformed.");

                PropertyActivator activator = JsonObjectNoexceptConverter<T>.MakeActivator(x, setterInfo, utf8JsonParser);

                activators.Add(nameAttribute.Name, activator);
            } // foreach (...)
        } // for (...)
    }

    public static bool IsSpecialized => s_activators.Count != 0;

    public static IReadOnlyDictionary<string, string> JsonNames => s_names;

    public static ImmutableDictionary<string, JsonPointer> MakeJsonPointers()
    {
        Dictionary<string, JsonPointer> pointerMap = new();

        foreach (KeyValuePair<string, string> x in s_names)
            pointerMap.Add(x.Key, new(x.Value));

        return new(pointerMap);
    }

    public override Utf8JsonParser<T> MakeParser(NullabilityAwareType<T> typeToConvert)
        => typeToConvert.IsNotNull
        ? JsonObjectNoexceptConverter<T>.TryGetNotNull
        : JsonObjectNoexceptConverter<T>.TryGetMaybeNull;

    public static bool TryGetNotNull(Utf8JsonReader json, [MaybeNullWhen(returnValue: false)] out T value)
        => JsonObjectNoexceptConverter<T>.TryGetNotNull(ref json, out value);

    public static bool TryGetMaybeNull(Utf8JsonReader json, out T? value)
        => JsonObjectNoexceptConverter<T>.TryGetMaybeNull(ref json, out value);

    private static bool TryGetUnchecked(ref Utf8JsonReader json, out T? value)
    {
        bool isGood = true;
        value = new();
        IResponsiveNoexceptJson? responsive = value as IResponsiveNoexceptJson;
        responsive?.OnDeserializing();

        HashSet<string> visited = new();
        HashSet<string> required = new(s_requiredProperties);

        while (json.Read() && json.TokenType != JsonTokenType.EndObject)
        {
            if (json.TokenType != JsonTokenType.PropertyName)
            {
                // Object malformed: jump to the matching '}'.
                json.FastForwardToEndObject();

                value = null;
                return false;
            } // if (...)

            string? propertyName = json.GetString();

            if (propertyName is null)
                isGood = false;

            // Move to property value.
            if (!json.Read())
            {
                value = null;
                return false;
            } // if (...)

            // Skip unrecognized properties.
            if (propertyName is null || !s_activators.TryGetValue(propertyName, out PropertyActivator? activator))
            {
                json.Skip();
                continue;
            } // if (...)

            // Duplicate property name encountered.
            if (!visited.Add(propertyName))
            {
                isGood = false;
                json.Skip();
                continue;
            } // if (...)

            required.Remove(propertyName);

            if (!activator.TryParseAndSet(value, propertyName, ref json))
            {
                isGood = false;
                responsive?.OnParsingFailure(propertyName, ref json);
                json.Skip();
                continue;
            } // if (...)
        } // while (...)

        // Guard against malformed JSON.
        if (json.TokenType != JsonTokenType.EndObject)
            isGood = false;

        foreach (string propertyName in required)
        {
            isGood = false;
            responsive?.OnRequiredPropertyMissing(propertyName);
        } // if (...)

        responsive?.OnDeserialized();
        return isGood;
    }

    public static bool TryGetNotNull(ref Utf8JsonReader json, [MaybeNullWhen(returnValue: false)] out T value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.StartObject:
                return JsonObjectNoexceptConverter<T>.TryGetUnchecked(ref json, out value);
            default:
                value = null;
                return false;
        } // switch (...)
    }

    public static bool TryGetMaybeNull(ref Utf8JsonReader json, out T? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.StartObject:
                return JsonObjectNoexceptConverter<T>.TryGetUnchecked(ref json, out value);
            default:
                value = null;
                return false;
        } // switch (...)
    }
}
