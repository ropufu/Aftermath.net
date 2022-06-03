namespace Ropufu;

public static class NumericExtenders
{
    public static bool IsFiniteOrNull<T>(this T? that)
        where T : unmanaged, ISpanFormattable
        => !that.HasValue || that.Value.IsFinite();

    public static bool IsFinite<T>(this T that)
        where T : unmanaged, ISpanFormattable
        => that switch
        {
            float f => float.IsFinite(f),
            double d => double.IsFinite(d),
            // Innocent unless proven otherwise.
            _ => true
        };

    public static bool IsIntegerValued(this float that)
        => float.IsFinite(that) && (Math.Floor(that) == Math.Ceiling(that));

    public static bool IsIntegerValued(this double that)
        => double.IsFinite(that) && (Math.Floor(that) == Math.Ceiling(that));

    public static bool IsIntegerValued(this decimal that)
        => (Math.Floor(that) == Math.Ceiling(that));
}
