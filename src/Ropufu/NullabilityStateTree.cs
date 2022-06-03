using System.Reflection;

namespace Ropufu;

/// <summary>
/// Stores either nullability read state or nullability write state across
/// the type definition hierarchy, allowing, for example, to distinguish
/// string?[] from string[], Typle`[T1?, T2] from Tuple`[T1, T2?], etc.
/// </summary>
public sealed class NullabilityStateTree : IEquatable<NullabilityStateTree>
{
    private const int Multiplier = 479001599;

    private readonly int _hash = 39916801;

    public NullabilityState State { get; private init; }

    public NullabilityStateTree? ElementType { get; private init; }

    public NullabilityStateTree[] GenericTypeArguments { get; private init; }

    private NullabilityStateTree(NullabilityState state, NullabilityStateTree? elementType)
        : this(state, elementType, Array.Empty<NullabilityStateTree>())
    {
    }

    private NullabilityStateTree(NullabilityState state, NullabilityStateTree? elementType, NullabilityStateTree[] typeArguments)
    {
        this.State = state;

        _hash = NullabilityStateTree.Multiplier * _hash + this.State.GetHashCode();

        this.ElementType = elementType;
        if (this.ElementType is not null)
            _hash = NullabilityStateTree.Multiplier * _hash + this.ElementType.GetHashCode();

        this.GenericTypeArguments = typeArguments;
    }

    public static NullabilityStateTree MakeSimple(NullabilityState state)
        => new(state, null);

    public NullabilityStateTree MakeArrayType(NullabilityState arrayState)
        => new(arrayState, this);

    public static NullabilityStateTree MakeGenericType(NullabilityState state, NullabilityStateTree[] typeArguments)
    {
        ArgumentNullException.ThrowIfNull(typeArguments);
        return new(state, null, typeArguments);
    }

    public static NullabilityStateTree CreateReadState(NullabilityInfo nullability)
        => new(nullability, false);

    public static NullabilityStateTree CreateWriteState(NullabilityInfo nullability)
        => new(nullability, true);

    private NullabilityStateTree(NullabilityInfo nullability, bool isWrite)
    {
        ArgumentNullException.ThrowIfNull(nullability);

        this.State = isWrite ? nullability.WriteState : nullability.ReadState;

        _hash = NullabilityStateTree.Multiplier * _hash + this.State.GetHashCode();

        if (nullability.ElementType is not null)
        {
            this.ElementType = new(nullability.ElementType, isWrite);
            _hash = NullabilityStateTree.Multiplier * _hash + this.ElementType.GetHashCode();
        } // if (...)

        NullabilityInfo[] argumentNullabilities = nullability.GenericTypeArguments;
        int n = argumentNullabilities.Length;

        this.GenericTypeArguments = new NullabilityStateTree[n];
        for (int i = 0; i < n; ++i)
        {
            NullabilityStateTree genericTypeArgument = new(argumentNullabilities[i], isWrite);
            this.GenericTypeArguments[i] = genericTypeArgument;
            _hash = NullabilityStateTree.Multiplier * _hash + genericTypeArgument.GetHashCode();
        } // for (...)
    }

    private bool ElementTypeEquals(NullabilityStateTree other)
    {
        if (this.ElementType is null)
            return other.ElementType is null;
        else
            return this.ElementType.Equals(other.ElementType);
    }

    private bool GenericTypeArgumentsEquals(NullabilityStateTree other)
    {
        int n = this.GenericTypeArguments.Length;

        if (other.GenericTypeArguments.Length != n)
            return false;

        for (int i = 0; i < n; ++i)
            if (this.GenericTypeArguments[i] != other.GenericTypeArguments[i])
                return false;

        return true;
    }

    public override bool Equals(object? other)
        => this.Equals(other as NullabilityStateTree);

    public bool Equals(NullabilityStateTree? other)
    {
        if (other is null)
            return false;

        return
            this.State == other.State &&
            this.ElementTypeEquals(other) &&
            this.GenericTypeArgumentsEquals(other);
    }

    public static bool operator ==(NullabilityStateTree x, NullabilityStateTree y)
        => x.Equals(y);

    public static bool operator !=(NullabilityStateTree x, NullabilityStateTree y)
        => !x.Equals(y);

    public override int GetHashCode() => _hash;
}
