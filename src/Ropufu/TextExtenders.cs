using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Ropufu;

[Flags]
public enum StringSnakeOptions
{
    None = 0,
    /// <summary>
    /// Reduce repeated underscores to a singe underscore.
    /// </summary>
    CollapseUnderscores = 1,
    /// <summary>
    /// Remove underscores at the start of the word.
    /// </summary>
    TrimStart = 2,
    /// <summary>
    /// Remove underscores at the end of the word.
    /// </summary>
    TrimEnd = 4,
    /// <summary>
    /// Remove underscores at the start and end of the word.
    /// </summary>
    Trim = TrimStart | TrimEnd
}

public static class TextExtenders
{
    public static bool IsRegex(this string that)
        => that.IsRegex(out _);

    public static bool IsRegex(this string that, [MaybeNullWhen(returnValue: false)] out Regex expression)
    {
        ArgumentNullException.ThrowIfNull(that);

        try
        {
            expression = new Regex(that);
            return true;
        } // try
        catch (ArgumentException)
        {
            expression = null;
            return false;
        } // catch (...)
    }

    public static void TrimAll(this IList<string> that, bool removeEmptyEntries = true)
    {
        ArgumentNullException.ThrowIfNull(that);

        int n = that.Count;
        for (int i = 0; i < n; ++i)
        {
            string trimmed = that[i].Trim();
            if (!removeEmptyEntries || trimmed.Length != 0)
                that[i] = trimmed;
            else
            {
                that.RemoveAt(i);
                --i;
                --n;
            } // else
        } // for (...)
    }

    /// <summary>
    /// Adds underscore between lower case--upper case letters, converts all letters to lower case, and replaces non-letter characters with underscores.
    /// </summary>
    public static string ToSnakeCase(this string that, StringSnakeOptions options = StringSnakeOptions.None)
    {
        ArgumentNullException.ThrowIfNull(that);

        StringBuilder builder = new(that.Length * 2);

        bool wasUnderscore = false;
        bool wasLowercase = false;
        foreach (char c in that)
        {
            bool isLowercase = char.IsLower(c);
            bool isUppercase = char.IsUpper(c);
            bool isNeither = false;

            if (isLowercase)
                builder.Append(c);
            else if (isUppercase)
            {
                if (wasLowercase)
                    builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            } // else if (...)
            else
            {
                isNeither = true;
                if (!wasUnderscore)
                    builder.Append('_');
                else if ((options & StringSnakeOptions.CollapseUnderscores) == StringSnakeOptions.None)
                    builder.Append('_');
            } // else

            wasUnderscore = isNeither;
            wasLowercase = isLowercase;
        } // foreach (...)

        if ((options & StringSnakeOptions.TrimStart) != StringSnakeOptions.None)
        {
            while (builder.Length != 0)
            {
                if (builder[0] == '_')
                    builder.Remove(0, 1);
                else
                    break;
            } // while (...)
        } // if (...)

        if ((options & StringSnakeOptions.TrimEnd) != StringSnakeOptions.None)
        {
            while (builder.Length != 0)
            {
                if (builder[^1] == '_')
                    builder.Remove(builder.Length - 1, 1);
                else
                    break;
            } // while (...)
        } // if (...)

        return builder.ToString();
    }

    public static string CapitalizeFirstLetter(this string that)
    {
        ArgumentNullException.ThrowIfNull(that);

        if (that.Length == 0)
            return that;
        
        StringBuilder builder = new(that.Length);
        builder.Append(char.ToUpperInvariant(that[0]));
        builder.Append(that.AsSpan(1));
        return builder.ToString();
    }

    public static StringBuilder AppendCarriageReturn(this StringBuilder that, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(that);

        return that.Append(value).Append('\r');
    }

    public static StringBuilder AppendLineFeed(this StringBuilder that, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(that);

        return that.Append(value).Append('\n');
    }

    public static StringBuilder AppendCarriageReturnLineFeed(this StringBuilder that, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(that);

        return that.Append(value).Append('\r').Append('\n');
    }
}
