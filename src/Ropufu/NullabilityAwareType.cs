using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ropufu;

/// <summary>
/// A type-checked wrapper around <see cref="NullabilityAwareType"/>.
/// </summary>
/// <typeparam name="T">Generic type corresponding to <see cref="NullabilityAwareType.Type"/>.</typeparam>
public sealed class NullabilityAwareType<T>
    : NullabilityAwareType, IEquatable<NullabilityAwareType<T>>
{
    private class NullabilityTreeExtractor
    {
        private static readonly T? s_value = default;
        private static readonly FieldInfo s_info = typeof(NullabilityTreeExtractor).GetField(nameof(s_value), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static NullabilityAwareType<T> Extract(NullabilityInfoContext context)
            => new(NullabilityStateTree.CreateReadState(context.Create(s_info)));
    }

    internal NullabilityAwareType(NullabilityStateTree nullabilityTree)
        : base(typeof(T), nullabilityTree)
    {
    }

    public static NullabilityAwareType<T> Unknown(NullabilityInfoContext? context = null)
        => NullabilityTreeExtractor.Extract(context ?? new());

    public new NullabilityAwareType<T[]> MakeArrayType(NullabilityState arrayState)
        => new(this.NullabilityTree.MakeArrayType(arrayState));

    public override bool Equals(object? other)
        => base.Equals(other);

    public bool Equals(NullabilityAwareType<T>? other)
        => base.Equals(other);

    public override int GetHashCode()
        => base.GetHashCode();
}

public class NullabilityAwareType
    : IEquatable<NullabilityAwareType>
{
    public Type Type { get; private init; }

    public NullabilityStateTree NullabilityTree { get; private init; }

    public NullabilityState State => this.NullabilityTree.State;

    public bool IsNotNull
        => this.NullabilityTree.State == NullabilityState.NotNull;

    protected internal NullabilityAwareType(Type type, NullabilityStateTree nullabilityTree)
    {
        this.Type = type;
        this.NullabilityTree = nullabilityTree;
    }

    public NullabilityAwareType<T> Promote<T>()
    {
        if (this.Type != typeof(T))
            throw new InvalidOperationException("Generic parameter [T] inconsistent with current type.");

        return new(this.NullabilityTree);
    }

    public NullabilityAwareType MakeArrayType(NullabilityState arrayState)
    {
        Type arrayType = this.Type.MakeArrayType();
        return new(arrayType, this.NullabilityTree.MakeArrayType(arrayState));
    }

    /// <exception cref="InvalidOperationException">Generic type definition expected.</exception>
    /// <exception cref="ArgumentException">Generic type definition inconsistent with the number of type arguments.</exception>
    public static NullabilityAwareType MakeGenericType(
        NullabilityState state,
        Type genericTypeDefinition,
        params NullabilityAwareType[] typeArguments)
    {
        ArgumentNullException.ThrowIfNull(genericTypeDefinition);
        ArgumentNullException.ThrowIfNull(typeArguments);

        int n = typeArguments.Length;

        Type[] simpleTypeArguments = new Type[n];
        NullabilityStateTree[] nullabilityTrees = new NullabilityStateTree[n];

        for (int i = 0; i < n; ++i)
        {
            NullabilityAwareType x = typeArguments[i];
            simpleTypeArguments[i] = x.Type;
            nullabilityTrees[i] = x.NullabilityTree;
        } // for (...)

        // The following line will throw when "natural" conditions are not met.
        Type genericType = genericTypeDefinition.MakeGenericType(simpleTypeArguments);
        return new(genericType, NullabilityStateTree.MakeGenericType(state, nullabilityTrees));
    }

    /// <exception cref="InvalidOperationException">Generic type expected for [T].</exception>
    /// <exception cref="ArgumentException">[T] inconsistent with the number of type arguments.</exception>
    public static NullabilityAwareType<T> MakeGenericType<T>(
        NullabilityState state,
        params NullabilityAwareType[] typeArguments)
    {
        ArgumentNullException.ThrowIfNull(typeArguments);

        int n = typeArguments.Length;
        Type genericType = typeof(T);

        if (!genericType.IsGenericType)
            throw new InvalidOperationException("Generic type expected for [T].");

        if (genericType.GetGenericArguments().Length != n)
            throw new ArgumentException("Generic parameter [T] inconsistent with the number of type arguments.", nameof(typeArguments));

        Type[] simpleTypeArguments = new Type[n];
        NullabilityStateTree[] nullabilityTrees = new NullabilityStateTree[n];

        for (int i = 0; i < n; ++i)
        {
            NullabilityAwareType x = typeArguments[i];
            simpleTypeArguments[i] = x.Type;
            nullabilityTrees[i] = x.NullabilityTree;
        } // for (...)

        return new(NullabilityStateTree.MakeGenericType(state, nullabilityTrees));
    }

    public NullabilityAwareType? GetElementType()
    {
        Type? elementType = this.Type.GetElementType();

        if (elementType is null)
            return null;
        else
        {
            if (this.NullabilityTree.ElementType is null)
                throw new InvalidOperationException("Nullability inconsistent with type definition.");

            return new(elementType, this.NullabilityTree.ElementType);
        } // else
    }

    /// <exception cref="InvalidOperationException">Nullability inconsistent with type definition.</exception>
    public NullabilityAwareType[] GetGenericArguments()
    {
        Type[] genericArgumentTypes = this.Type.GetGenericArguments();
        NullabilityStateTree[] argumentNullabilities = this.NullabilityTree.GenericTypeArguments;

        int n = genericArgumentTypes.Length;
        NullabilityAwareType[] result = new NullabilityAwareType[n];

        if (genericArgumentTypes.Length != argumentNullabilities.Length)
            throw new InvalidOperationException("Nullability inconsistent with type definition.");

        for (int i = 0; i < n; ++i)
            result[i] = new(genericArgumentTypes[i], argumentNullabilities[i]);

        return result;
    }

    /// <typeparam name="T">Simple value type; that is, it is a closed non-generic type.</typeparam>
    /// <exception cref="NotSupportedException">Type does not satisfy simple type constraints.</exception>
    public static NullabilityAwareType<T?> MakeNullable<T>()
        where T : struct
    {
        Type type = typeof(T);

        if (type.ContainsGenericParameters)
            throw new NotSupportedException("Closed type expected.");

        if (type.IsGenericType)
            throw new NotSupportedException("Non-generic type expected.");

        return new(NullabilityStateTree.MakeSimple(NullabilityState.Nullable));
    }

    /// <typeparam name="T">Simple value type; that is, it is a closed non-generic type.</typeparam>
    /// <exception cref="NotSupportedException">Type does not satisfy simple type constraints.</exception>
    public static NullabilityAwareType<T> MakeSimple<T>()
        where T : struct
    {
        Type type = typeof(T);

        if (type.ContainsGenericParameters)
            throw new NotSupportedException("Closed type expected.");

        if (type.IsGenericType)
            throw new NotSupportedException("Non-generic type expected.");

        return new(NullabilityStateTree.MakeSimple(NullabilityState.NotNull));
    }

    /// <typeparam name="T">
    /// Simple reference type; that is, it is a closed non-abstract non-generic type
    /// that does not encompass or refer to another type.
    /// </typeparam>
    /// <exception cref="NotSupportedException">Type does not satisfy simple type constraints.</exception>
    public static NullabilityAwareType<T> MakeSimple<T>(NullabilityState state)
        where T : class
    {
        Type type = typeof(T);

        if (type.ContainsGenericParameters || type.IsAbstract)
            throw new NotSupportedException("Closed non-abstract type expected.");

        if (type.IsGenericType)
            throw new NotSupportedException("Non-generic type expected.");

        if (type.HasElementType)
            throw new NotSupportedException("Expecting a type that does not encompass or refer to another type.");

        return new(NullabilityStateTree.MakeSimple(state));
    }

    public static NullabilityAwareType FromPropertyGetter(PropertyInfo property, NullabilityInfoContext nullabilityContext)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(nullabilityContext);

        return new NullabilityAwareType(
            property.PropertyType,
            NullabilityStateTree.CreateReadState(nullabilityContext.Create(property)));
    }

    public static NullabilityAwareType FromPropertySetter(PropertyInfo property, NullabilityInfoContext nullabilityContext)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(nullabilityContext);

        return new NullabilityAwareType(
            property.PropertyType,
            NullabilityStateTree.CreateWriteState(nullabilityContext.Create(property)));
    }

    public static NullabilityAwareType FromField(FieldInfo field, NullabilityInfoContext nullabilityContext)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(nullabilityContext);

        // @todo Check if ReadState and WriteState are always the same for fields.
        return new NullabilityAwareType(
            field.FieldType,
            NullabilityStateTree.CreateReadState(nullabilityContext.Create(field)));
    }

    public override bool Equals(object? other)
        => this.Equals(other as NullabilityAwareType);

    public bool Equals(NullabilityAwareType? other)
    {
        if (other is null)
            return false;

        return
            this.Type == other.Type &&
            this.NullabilityTree == other.NullabilityTree;
    }

    public override int GetHashCode()
        => (this.Type, this.NullabilityTree).GetHashCode();

    [return: NotNullIfNotNull("that")]
    public static explicit operator Type?(NullabilityAwareType? that)
        => that?.Type;
}
