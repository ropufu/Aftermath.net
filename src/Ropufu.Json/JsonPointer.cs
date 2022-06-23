using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ropufu.Json;

/// <summary>
/// An immutable class representing JSON Pointer.
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc6901"/>
/// </summary>
public class JsonPointer
{
    private readonly string[] _referenceTokens;

    private JsonPointer(int n)
        => _referenceTokens = new string[n];

    /// <summary>
    /// Represents the whole document.
    /// </summary>
    public JsonPointer()
        => _referenceTokens = Array.Empty<string>();

    /// <summary>
    /// Constructs a JSON Pointer from a sequence of tokens.
    /// </summary>
    /// <param name="tokens">Unescaped tokens.</param>
    /// <remarks>
    /// <see cref="JsonPointer.Length"/> will equal the number of <paramref name="tokens"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="tokens"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="tokens"/> contains null elements.</exception>
    public JsonPointer(params string[] tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        _referenceTokens = new string[tokens.Length];
        
        for (int i = 0; i < tokens.Length; ++i)
        {
            if (tokens[i] is null)
                throw new ArgumentException(Literals.ExpectedNotNullItems, nameof(tokens));

            _referenceTokens[i] = JsonPointer.EscapeUnchecked(tokens[i]);
        } // for (...)
    }
    
    /// <exception cref="FormatException"></exception>
    public static JsonPointer Parse(string value)
    {
        if (JsonPointer.TryParse(value, out JsonPointer? result))
            return result;
        else
            throw new FormatException();
    }

    public static bool TryParse(string value, [MaybeNullWhen(returnValue: false)] out JsonPointer result)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            result = new();
            return true;
        } // if (...)

        if (value[0] != '/')
        {
            result = null;
            return false;
        } // if (...)

        result = null;
        string[] tokens = value[1..].Split('/');

        foreach (string x in tokens)
            if (!JsonPointer.TryUnescape(x))
                return false;

        result = new(tokens.Length);
        Array.Copy(tokens, result._referenceTokens, tokens.Length);
        return true;
    }

    /// <summary>
    /// Composes two JSON Pointer into one.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="a"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="b"/> is null.</exception>
    /// <example>Composition of "/a/b" and "/c" will be "/a/b/c".</example>
    public static JsonPointer Compose(JsonPointer a, JsonPointer b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        int m = a._referenceTokens.Length;
        int n = b._referenceTokens.Length;
        JsonPointer result = new(m + n);

        Array.Copy(a._referenceTokens, 0, result._referenceTokens, 0, m);
        Array.Copy(b._referenceTokens, 0, result._referenceTokens, m, n);

        return result;
    }
    public static JsonPointer operator +(JsonPointer a, JsonPointer b)
        => JsonPointer.Compose(a, b);

    public int Length
        => _referenceTokens.Length;

    /// <summary>
    /// Retrieves the (unescaped) reference token at the specified position.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>Unescaped reference token.</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public string this[int index]
    {
        get
        {
            if (index < 0 || index >= _referenceTokens.Length)
                throw new IndexOutOfRangeException();

            JsonPointer.TryUnescape(_referenceTokens[index], out string? result);
            return result!;
        }
    }

    /// <summary>
    /// Because the characters '~' and '/' have special meanings in JSON
    /// Pointer, '~' needs to be encoded as "~0" and '/' needs to be
    /// encoded as "~1" when these characters appear in a reference token.
    /// </summary>
    /// <param name="value">Unescaped single reference token.</param>
    /// <returns>JSON Pointer value corresponding to <paramref name="value"/>.</returns>
    /// <example>
    /// "a/b" will be encoded as "a~1b".
    /// "m~n" will be encoded as "m~0n".
    /// </example>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonPointer.EscapeUnchecked(value);
    }

    private static string EscapeUnchecked(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder builder = new(capacity: value.Length * 2);

        for (int i = 0; i < value.Length; ++i)
        {
            char c = value[i];
            switch (c)
            {
                case '~':
                    builder.Append('~').Append('0');
                    break;
                case '/':
                    builder.Append('~').Append('1');
                    break;
                default:
                    builder.Append(c);
                    break;
            } // switch (...)
        } // for (...)

        return builder.ToString();
    }

    /// <summary>
    /// Checks if <paramref name="value"/> is a legitimate reference token in a JSON Pointer.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryUnescape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        for (int i = 0; i < value.Length; ++i)
        {
            switch (value[i])
            {
                case '/':
                    return false;
                case '~':
                    if (++i == value.Length)
                        return false;

                    switch (value[i])
                    {
                        case '0':
                        case '1':
                            break;
                        default:
                            return false;
                    } // switch (...)
                    break;
            } // switch (...)
        } // for (...)

        return true;
    }

    /// <summary>
    /// Tries to translate a reference token from its JSON Pointer
    /// representation to the underlying JSON string.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <example>
    /// "a~1bb" will be decoded as "a/b".
    /// "m~0n" will be decoded as "m~n".
    /// Decoding of "a/b" will fail.
    /// Decoding of "m~n" will fail.
    /// </example>
    public static bool TryUnescape(string value, [MaybeNullWhen(returnValue: false)] out string result)
    {
        ArgumentNullException.ThrowIfNull(value);

        result = null;
        StringBuilder builder = new(capacity: value.Length);

        for (int i = 0; i < value.Length; ++i)
        {
            switch (value[i])
            {
                case '/':
                    return false;
                case '~':
                    if (++i == value.Length)
                        return false;

                    switch (value[i])
                    {
                        case '0':
                            builder.Append('~');
                            break;
                        case '1':
                            builder.Append('/');
                            break;
                        default:
                            return false;
                    } // switch (...)
                    break;
                default:
                    builder.Append(value[i]);
                    break;
            } // switch (...)
        } // for (...)

        result = builder.ToString();
        return true;
    }

    public override string ToString()
    {
        StringBuilder builder = new();

        for (int i = 0; i < _referenceTokens.Length; ++i)
            builder.Append('/').Append(_referenceTokens[i]);

        return builder.ToString();
    }
}
