namespace Ropufu.Json;

public abstract class NoexceptJsonConverterBase
{
    protected internal NoexceptJsonConverterBase()
    {
    }

    public abstract bool CanConvert(Type typeToConvert);

    /// <summary>
    /// </summary>
    /// <param name="typeToConvert"></param>
    /// <returns><see cref="Utf8JsonParser{T}"/></returns>
    public abstract Delegate MakeUtf8JsonParser(NullabilityAwareType typeToConvert);
}

public abstract class NoexceptJsonConverterFactory
    : NoexceptJsonConverterBase
{
    public abstract NoexceptJsonConverter CreateConverter(NullabilityAwareType typeToConvert);

    public sealed override Delegate MakeUtf8JsonParser(NullabilityAwareType typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        NoexceptJsonConverter converter = this.CreateConverter(typeToConvert);

        if (converter is null)
            throw new NotSupportedException("Converter factory should create non-null instances.");

        return converter.MakeUtf8JsonParser(typeToConvert);
    }
}

public abstract class NoexceptJsonConverter
    : NoexceptJsonConverterBase
{
    protected internal NoexceptJsonConverter()
    {
    }
}

public abstract class NoexceptJsonConverter<T>
    : NoexceptJsonConverter
{
    public sealed override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert == typeof(T);
    }

    public abstract Utf8JsonParser<T> MakeParser(NullabilityAwareType<T> typeToConvert);

    public sealed override Delegate MakeUtf8JsonParser(NullabilityAwareType typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (!this.CanConvert(typeToConvert.Type))
            throw new NotSupportedException("Converter type mismatch.");

        return this.MakeParser(typeToConvert.Promote<T>());
    }
}
