namespace Ropufu;

public static class MiscExtenders
{
    public static bool IsDistinct<T>(this IEnumerable<T> that)
    {
        ArgumentNullException.ThrowIfNull(that);

        HashSet<T> set = new();
        
        foreach (T x in that)
            if (!set.Add(x))
                return false;

        return true;
    }

    public static bool IsDistinct<T>(this List<T> that)
    {
        ArgumentNullException.ThrowIfNull(that);

        HashSet<T> set = new(capacity: that.Count);

        foreach (T x in that)
            if (!set.Add(x))
                return false;

        return true;
    }
}
