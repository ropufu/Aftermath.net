using System.Text.Json;

namespace Ropufu.Json;

public static partial class NoexceptJson
{
    public static bool TryGetBoolean(ref Utf8JsonReader json, out bool value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.True:
                value = true;
                return true;
            case JsonTokenType.False:
                value = false;
                return true;
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetBoolean(ref Utf8JsonReader json, out bool? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.True:
                value = true;
                return true;
            case JsonTokenType.False:
                value = false;
                return true;
            default:
                value = default;
                return false;
        } // switch (...)
    }

    public static bool TryGetByte(ref Utf8JsonReader json, out byte value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetByte(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetByte(ref Utf8JsonReader json, out byte? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetByte(out byte x))
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

    public static bool TryGetInt16(ref Utf8JsonReader json, out short value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetInt16(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetInt16(ref Utf8JsonReader json, out short? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetInt16(out short x))
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

    public static bool TryGetInt32(ref Utf8JsonReader json, out int value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetInt32(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetInt32(ref Utf8JsonReader json, out int? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetInt32(out int x))
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

    public static bool TryGetInt64(ref Utf8JsonReader json, out long value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetInt64(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetInt64(ref Utf8JsonReader json, out long? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetInt64(out long x))
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

    public static bool TryGetIntPtr(ref Utf8JsonReader json, out nint value)
    {
        switch (IntPtr.Size)
        {
            case 4:
                if (json.TokenType == JsonTokenType.Number && json.TryGetInt32(out int x))
                {
                    value = (nint)x;
                    return true;
                } // if (...)
                break;
            case 8:
                if (json.TokenType == JsonTokenType.Number && json.TryGetInt64(out long y))
                {
                    value = (nint)y;
                    return true;
                } // if (...)
                break;
        } // switch (...)

        value = default;
        return false;
    }

    public static bool TryGetIntPtr(ref Utf8JsonReader json, out nint? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (IntPtr.Size == 4 && json.TryGetInt32(out int x))
                {
                    value = (nint)x;
                    return true;
                } // if (...)
                else if (IntPtr.Size == 8 && json.TryGetInt64(out long y))
                {
                    value = (nint)y;
                    return true;
                } // else if (...)
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

    public static bool TryGetSingle(ref Utf8JsonReader json, out float value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetSingle(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetSingle(ref Utf8JsonReader json, out float? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetSingle(out float x))
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

    public static bool TryGetDouble(ref Utf8JsonReader json, out double value)
    {
        if (json.TokenType == JsonTokenType.Number && json.TryGetDouble(out value))
            return true;

        value = default;
        return false;
    }

    public static bool TryGetDouble(ref Utf8JsonReader json, out double? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.Number:
                if (json.TryGetDouble(out double x))
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

    public static bool TryGetChar(ref Utf8JsonReader json, out char value)
    {
        if (json.TokenType == JsonTokenType.String)
        {
            string x = json.GetString()!;
            if (x.Length == 1)
            {
                value = x[0];
                return true;
            } // if (...)
        } // if (...)

        value = default;
        return false;
    }

    public static bool TryGetChar(ref Utf8JsonReader json, out char? value)
    {
        switch (json.TokenType)
        {
            case JsonTokenType.Null:
                value = null;
                return true;
            case JsonTokenType.String:
                string x = json.GetString()!;
                if (x.Length == 1)
                {
                    value = x[0];
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
}
