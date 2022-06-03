using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ropufu.Json;

public static partial class NoexceptJson
{
    public static bool TryGetDecimal(ref Utf8JsonReader json, out decimal value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetDecimal(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetDecimal(ref Utf8JsonReader json, out decimal? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetDecimal(out decimal x))
                {
                    value = x;
                    return true;
                } // if (...)
                else
                {
                    value = default;
                    return false;
                } // else
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetGuid(ref Utf8JsonReader json, out Guid value)
    {
        if (json.TokenType == JsonTokenType.String && json.TryGetGuid(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetGuid(ref Utf8JsonReader json, out Guid? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetGuid(out Guid x))
                {
                    value = x;
                    return true;
                } // if (...)
                else
                {
                    value = default;
                    return false;
                } // else
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetDateTime(ref Utf8JsonReader json, out DateTime value)
    {
        if (json.TokenType == JsonTokenType.String && json.TryGetDateTime(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetDateTime(ref Utf8JsonReader json, out DateTime? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetDateTime(out DateTime x))
                {
                    value = x;
                    return true;
                } // if (...)
                else
                {
                    value = default;
                    return false;
                } // else
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetDateTimeOffset(ref Utf8JsonReader json, out DateTimeOffset value)
    {
        if (json.TokenType == JsonTokenType.String && json.TryGetDateTimeOffset(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetDateTimeOffset(ref Utf8JsonReader json, out DateTimeOffset? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetDateTimeOffset(out DateTimeOffset x))
                {
                    value = x;
                    return true;
                } // if (...)
                else
                {
                    value = default;
                    return false;
                } // else
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetJsonElement(ref Utf8JsonReader json, out JsonElement value)
    {
        if (JsonElement.TryParseValue(ref json, out JsonElement? x))
        {
            value = x.Value;
            return true;
        } // if (...)

        value = default;
        return false;
    }

    public static bool TryGetNotNullString(ref Utf8JsonReader json, out string value)
    {
        if (json.TokenType == JsonTokenType.String)
        {
            value = json.GetString()!;
            return true;
        } // if (...)

        value = string.Empty;
        return false;
    }

    public static bool TryGetNullableString(ref Utf8JsonReader json, out string? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
            case JsonTokenType.String:
                value = json.GetString();
                return true;
        } // switch (...)

        value = default;
        return false;
    }

    public static bool TryGetNotNullRegex(ref Utf8JsonReader json, out Regex value)
    {
        if (json.TokenType == JsonTokenType.String)
        {
            if (json.GetString()!.IsRegex(out Regex? x))
            {
                value = x;
                return true;
            } // if (...)
        } // if (...)

        value = new(string.Empty);
        return false;
    }

    public static bool TryGetNullableRegex(ref Utf8JsonReader json, out Regex? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.String:
                if (json.GetString()!.IsRegex(out value))
                    return true;
                break;
        } // switch (...)

        value = default;
        return false;
    }
}
