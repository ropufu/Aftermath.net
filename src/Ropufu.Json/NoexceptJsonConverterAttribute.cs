using System.Reflection;

namespace Ropufu.Json;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property)]
public sealed class NoexceptJsonConverterAttribute : Attribute
{
    private readonly NoexceptJsonConverterBase _converter;

    /// <exception cref="ArgumentException">Converter malformed.</exception>
    public NoexceptJsonConverterAttribute(Type converterType)
    {
        ArgumentNullException.ThrowIfNull(converterType);

        if (converterType.ContainsGenericParameters || converterType.IsAbstract)
            throw new ArgumentException(Literals.ExpectedClosedNonAbstractType, nameof(converterType));

        if (converterType.GetConstructor(Type.EmptyTypes) is null)
            throw new ArgumentException(Literals.ExpectedDefaultConstructibleType, nameof(converterType));

        for (Type? x = converterType.BaseType; x is not null; x = x.BaseType)
        {
            if (x == typeof(NoexceptJsonConverterBase))
            {
                _converter = (NoexceptJsonConverterBase)Activator.CreateInstance(converterType)!;
                break;
            } // if (...)
        } // for (...)

        if (_converter is null)
            throw new ArgumentException("Converter should inherit from NoexceptJsonConverter`[T] or NoexceptJsonConverterFactory.", nameof(converterType));
    }

    public NoexceptJsonConverterBase Converter => _converter;

    public bool CanConvert(Type typeToConvert) => _converter.CanConvert(typeToConvert);

    public Delegate MakeUtf8JsonParser(NullabilityAwareType typeToConvert)
        => _converter.MakeUtf8JsonParser(typeToConvert);
}
