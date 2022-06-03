using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Ropufu;

public static class ReflectionExtenders
{
    private static object? Dynamic(this object that, string propertyName)
    {
        Type type = that.GetType();
        return type.GetProperty(propertyName)!.GetValue(that);
    }

    private static void Enforce(this NullabilityInfo instanceNullability, object? instance, string propertyName)
    {
        if (instance is null)
            if (instanceNullability.ReadState == NullabilityState.NotNull)
                throw new ArgumentException("Nullability context not aligned with existing value", propertyName);
            else
                return;

        // This is an array.
        if (instanceNullability.ElementType is not null)
        {
            foreach (object? element in (Array)instance)
                instanceNullability.ElementType.Enforce(element, propertyName);
            return;
        } // if (...)

        // This is IEnumerable<>.
        int enumerableTypeIndex = instanceNullability.Type.GetEnumerableDefinitionIndex();

        if (enumerableTypeIndex != -1)
        {
            NullabilityInfo typeArgumentNullability = instanceNullability.GenericTypeArguments[enumerableTypeIndex];
            foreach (object? x in (IEnumerable)instance)
                typeArgumentNullability.Enforce(x, propertyName);
            return;
        } // if (...)

        // This is Dictionary<,>.
        KeyValuePair<int, int> dictionaryTypeIndices = instanceNullability.Type.GetDictionaryDefinitionIndices();
        const string keyPropertyName = nameof(KeyValuePair<object, object>.Key);
        const string valuePropertyName = nameof(KeyValuePair<object, object>.Value);

        if (dictionaryTypeIndices.Key != -1)
        {
            NullabilityInfo keyTypeArgumentNullability = instanceNullability.GenericTypeArguments[dictionaryTypeIndices.Key];
            foreach (object x in (IEnumerable)instance)
                keyTypeArgumentNullability.Enforce(x.Dynamic(keyPropertyName), propertyName);
        } // if (...)

        if (dictionaryTypeIndices.Value != -1)
        {
            NullabilityInfo valueTypeArgumentNullability = instanceNullability.GenericTypeArguments[dictionaryTypeIndices.Value];
            foreach (object x in (IEnumerable)instance)
                valueTypeArgumentNullability.Enforce(x.Dynamic(valuePropertyName), propertyName);
        } // if (...)
    }

    /// <exception cref="ArgumentOutOfRangeException">
    /// Encountered certain nested templated classes with Dictionary<_, List<T>>-style properties.
    /// See https://github.com/dotnet/runtime/issues/68461 for a more detailed discussion.
    /// </exception>
    public static void Enforce<T>(this NullabilityInfoContext context, T instance)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(instance);

        foreach (PropertyInfo x in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            MethodInfo? getter = x.GetGetMethod();

            if (getter is null || getter.GetParameters().Length != 0)
                continue;

            object? propertyValue = getter.Invoke(instance, null);
            NullabilityInfo propertyNullability = context.Create(x);
            propertyNullability.Enforce(propertyValue, x.Name);
        } // foreach (...)
    }

    public static KeyValuePair<int, int> GetDictionaryDefinitionIndices(this Type that)
    {
        ArgumentNullException.ThrowIfNull(that);

        int keyIndex = -1;
        int valueIndex = -1;

        IReadOnlyDictionary<string, int> typeParameterMap = that.GetGenericTypeParameterMap(out TypeInfo definition);
        if (typeParameterMap.Count == 0)
            return new(keyIndex, valueIndex);

        foreach (Type x in definition.GetInterfaces())
        {
            if (!x.IsGenericType)
                continue;

            if (x.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            Type itemType = x.GenericTypeArguments[0];

            if (!itemType.IsGenericType)
                continue;

            if (itemType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                continue;

            Type keyType = itemType.GetGenericArguments()[0];
            Type valueType = itemType.GetGenericArguments()[1];

            if (!typeParameterMap.TryGetValue(keyType.Name, out keyIndex))
                keyIndex = -1;

            if (!typeParameterMap.TryGetValue(valueType.Name, out valueIndex))
                valueIndex = -1;

            return new(keyIndex, valueIndex);
        } // foreach (...)

        return new(keyIndex, valueIndex);
    }

    public static int GetEnumerableDefinitionIndex(this Type that)
    {
        ArgumentNullException.ThrowIfNull(that);

        IReadOnlyDictionary<string, int> typeParameterMap = that.GetGenericTypeParameterMap(out TypeInfo definition);
        if (typeParameterMap.Count == 0)
            return -1;

        foreach (Type x in definition.GetInterfaces())
        {
            if (!x.IsGenericType)
                continue;

            if (x.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            Type itemType = x.GenericTypeArguments[0];

            if (typeParameterMap.TryGetValue(itemType.Name, out int result))
                return result;
            else
                return -1;
        } // foreach (...)

        return -1;
    }

    private static IReadOnlyDictionary<string, int> GetGenericTypeParameterMap(this Type that, out TypeInfo definition)
    {
        definition = null!;

        if (!that.IsGenericType)
            return new Dictionary<string, int>(capacity: 0);

        definition = that.GetGenericTypeDefinition().GetTypeInfo();
        Type[] parameterTypes = definition.GenericTypeParameters;

        Dictionary<string, int> typeParameterMap = new(capacity: parameterTypes.Length);
        for (int i = 0; i < parameterTypes.Length; ++i)
            typeParameterMap.Add(parameterTypes[i].Name, i);

        return typeParameterMap;
    }

    public static string? GetJsonPropertyName(this Type that, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(that);
        ArgumentNullException.ThrowIfNull(propertyName);

        PropertyInfo? info = that.GetProperty(propertyName);

        if (info is null)
            throw new ArgumentOutOfRangeException(nameof(propertyName));

        JsonPropertyNameAttribute? nameAttribute = info.GetCustomAttribute<JsonPropertyNameAttribute>(false);
        return nameAttribute?.Name;
    }

    public static bool HasCustomAttribute<T>(this Type that, [MaybeNullWhen(returnValue: false)] out T attribute)
        where T : Attribute
    {
        ArgumentNullException.ThrowIfNull(that);

        attribute = that.GetCustomAttribute<T>();
        return attribute is not null;
    }

    public static T? DefaultConstruct<T>(this Type that)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(that);
        
        ConstructorInfo? info = that.GetConstructor(Array.Empty<Type>());
        if (info is null)
            return null;
        return (T)info.Invoke(null);
    }

    public static bool DoesImplement(this Type that, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(that);
        ArgumentNullException.ThrowIfNull(interfaceType);

        if (!interfaceType.IsInterface)
            throw new ArgumentException("Expecting an interface type.", nameof(interfaceType));

        if (interfaceType.IsGenericTypeDefinition)
        {
            foreach (Type x in that.GetInterfaces())
                if (x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType)
                    return true;
        } // if (...)
        else
        {
            foreach (Type x in that.GetInterfaces())
                if (x == interfaceType)
                    return true;
        } // else (...)

        return false;
    }

    public static bool DoesInherit(this Type that, Type other)
    {
        ArgumentNullException.ThrowIfNull(that);
        ArgumentNullException.ThrowIfNull(other);

        if (other.IsGenericTypeDefinition)
        {
            for (Type? x = that; x is not null; x = x.BaseType)
                if (x.IsGenericType && x.GetGenericTypeDefinition() == other)
                    return true;
        } // if (...)
        else
        {
            for (Type? x = that; x is not null; x = x.BaseType)
                if (x == other)
                    return true;
        } // else

        return false;
    }
}
