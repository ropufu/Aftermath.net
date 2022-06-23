using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ropufu.Json;

public partial class BasicSchema<TSchema>
{
    public static readonly TSchema TrivialTrue;
    public static readonly TSchema TrivialFalse;

    /// <summary>
    /// Maps property names to their JSON Pointer equivalents.
    /// </summary>
    protected static readonly IReadOnlyDictionary<string, JsonPointer> s_jsonPointers
        = JsonObjectNoexceptConverter<TSchema>.MakeJsonPointers();

    private static readonly List<PropertyGetter<TSchema?>> s_schemaGetters = new();
    private static readonly List<PropertyGetter<IEnumerable<TSchema?>?>> s_collectinGetters = new();
    private static readonly List<PropertyGetter<IReadOnlyDictionary<string, TSchema?>?>> s_dictionaryGetters = new();

    private class PropertyGetter<T>
    {
        public JsonPointer Address { get; private init; }

        public Func<TSchema, T?> Getter { get; private init; }

        public PropertyGetter(JsonPointer address, Delegate getter)
        {
            this.Address = address;
            this.Getter = (Func<TSchema, T?>)getter;
        }
    }

    static BasicSchema()
    {
        Type schemaType = typeof(TSchema);
        Type enumerableType = typeof(IEnumerable<TSchema?>);
        Type dictionaryType = typeof(IReadOnlyDictionary<string, TSchema?>);

        Type schemaGetterType = typeof(Func<,>).MakeGenericType(schemaType, schemaType);
        Type enumerableGetterType = typeof(Func<,>).MakeGenericType(schemaType, enumerableType);
        Type dictionaryGetterType = typeof(Func<,>).MakeGenericType(schemaType, dictionaryType);

        foreach (PropertyInfo x in schemaType.GetProperties())
        {
            MethodInfo? getterInfo = x.GetGetMethod();

            if (getterInfo is null)
                continue;

            if (!s_jsonPointers.TryGetValue(x.Name, out JsonPointer? pointer))
                continue;

            Type propertyType = x.PropertyType;

            if (propertyType == schemaType)
                s_schemaGetters.Add(new(pointer, Delegate.CreateDelegate(schemaGetterType, getterInfo)));
            else if (propertyType.DoesImplement(enumerableType))
                s_collectinGetters.Add(new(pointer, Delegate.CreateDelegate(enumerableGetterType, getterInfo)));
            else if (propertyType.DoesImplement(dictionaryType))
                s_dictionaryGetters.Add(new(pointer, Delegate.CreateDelegate(dictionaryGetterType, getterInfo)));
        } // foreach (...)

        TSchema trivialTrue = new() { _isTrivialTrue = true };
        TSchema trivialFalse = new() { _isTrivialFalse = true };

        BasicSchema<TSchema>.TrivialTrue = trivialTrue;
        BasicSchema<TSchema>.TrivialFalse = trivialFalse;
    }

    /// <summary>
    /// Maps the immediate schemas (properties that are either
    /// <typeparamref name="TSchema"/>-, <see cref="IEnumerable{}">IEnumerable</see>&lt;<typeparamref name="TSchema"/>&gt;-, or
    /// <see cref="IReadOnlyDictionary{,}">IReadOnlyDictionary</see>&lt;<see cref="string"/>, <typeparamref name="TSchema"/>&gt;-valued)
    /// by their JSON Pointer.
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    private static Dictionary<string, TSchema> InitializeImmediateSchemas(TSchema that)
    {
        Dictionary<string, TSchema> schemaMap = new();

        foreach (PropertyGetter<TSchema?> x in s_schemaGetters)
        {
            TSchema? schema = x.Getter(that);

            if (schema is not null)
                schemaMap.Add(x.Address.ToString(), schema);
        } // foreach (...)

        foreach (PropertyGetter<IEnumerable<TSchema?>?> x in s_collectinGetters)
        {
            IEnumerable<TSchema?>? schemas = x.Getter(that);

            if (schemas is null)
                continue;

            int index = 0;
            foreach (TSchema? schema in schemas)
            {
                if (schema is null)
                    continue;

                JsonPointer address = x.Address + new JsonPointer(index.ToString());
                schemaMap.Add(address.ToString(), schema);
                ++index;
            } // foreach (...)
        } // foreach (...)

        foreach (PropertyGetter<IReadOnlyDictionary<string, TSchema?>?> x in s_dictionaryGetters)
        {
            IReadOnlyDictionary<string, TSchema?>? namedSchemas = x.Getter(that);

            if (namedSchemas is null)
                continue;

            foreach (KeyValuePair<string, TSchema?> namedSchema in namedSchemas)
            {
                if (namedSchema.Value is null)
                    continue;

                JsonPointer address = x.Address + new JsonPointer(namedSchema.Key);
                schemaMap.Add(address.ToString(), namedSchema.Value);
            } // foreach (...)
        } // foreach (...)

        return schemaMap;
    }

    private static void CheckCircularReferences(IReadOnlyDictionary<string, TSchema> schemas, JsonLogger logger)
    {
        foreach (KeyValuePair<string, TSchema> x in schemas)
        {
            HashSet<object> trace = new();
            TSchema current = x.Value;
            while (current.ResolvedReference is not null)
            {
                if (!trace.Add(current))
                {
                    logger.Log(
                        $"Circular JSON reference originating at \"#{x.Key}\" encountered.",
                        MessageLevel.Error);
                    break;
                } // if (...)

                current = current.ResolvedReference;
            } // while (...)
        } // foreach (...)
    }

    private static void MapLocalAnchors(
        IReadOnlyDictionary<string, TSchema> schemas,
        out Dictionary<string, string> anchorPointers,
        out List<TSchema> unresolvedSchemas,
        JsonLogger logger)
    {
        anchorPointers = new() { { "#", "" } };
        unresolvedSchemas = new();

        foreach (KeyValuePair<string, TSchema> x in schemas)
        {
            TSchema child = x.Value;

            if (child.StaticAnchor is not null)
                if (!anchorPointers.TryAdd("#" + child.StaticAnchor, x.Key))
                    logger.Log(
                        $"Anchor \"#{child.StaticAnchor}\" cannot be redefined.",
                        MessageLevel.Error,
                        JsonPointer.Parse(x.Key));

            if (child.DynamicAnchor is not null)
                if (!anchorPointers.TryAdd("#" + child.DynamicAnchor, x.Key))
                    logger.Log(
                        $"Anchor \"#{child.DynamicAnchor}\" cannot be redefined.",
                        MessageLevel.Error,
                        JsonPointer.Parse(x.Key));

            if (child.IsLocalStaticReference || child.IsLocalDynamicReference)
                unresolvedSchemas.Add(child);
        } // foreach (...)
    }

    private static void ResolveLocalReferences(
        IReadOnlyDictionary<string, TSchema> schemas,
        IReadOnlyDictionary<string, string> anchorPointers,
        IEnumerable<TSchema> unresolvedSchemas,
        JsonLogger logger)
    {
        foreach (TSchema x in unresolvedSchemas)
        {
            JsonPointer augmentedAddress = JsonPointer.Parse("/" + x.StaticReference!);
            foreach (KeyValuePair<string, string> mapping in anchorPointers)
            {
                if (augmentedAddress[0] == mapping.Key)
                {
                    string resolvedAddress = mapping.Value + x.StaticReference![mapping.Key.Length..];

                    if (schemas.TryGetValue(resolvedAddress, out TSchema? reference))
                        x.ResolvedReference = reference;

                    break;
                } // if (...)
            } // foreach (...)

            if (x.ResolvedReference is null)
                logger.Log(
                    $"Reference \"#{x.StaticReference!}\" could not be resolved.",
                    MessageLevel.Error);
        } // foreach (...)
    }
}
