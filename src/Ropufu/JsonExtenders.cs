using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Ropufu;

public static class JsonExtenders
{
    public static List<JsonElement> GetArrayAsList(this JsonElement that)
    {
        if (!that.TryGetArrayAsList(out List<JsonElement>? value))
            throw new InvalidOperationException("This value's ValueKind is not Array.");

        return value;
    }

    public static bool TryGetArrayAsList(this JsonElement that, [MaybeNullWhen(returnValue: false)] out List<JsonElement> value)
    {
        if (that.ValueKind != JsonValueKind.Array)
        {
            value = null;
            return false;
        } // if (...)

        value = new();
        using JsonElement.ArrayEnumerator enumerator = that.EnumerateArray();

        while (enumerator.MoveNext())
            value.Add(enumerator.Current);

        return true;
    }
    public static List<KeyValuePair<string, JsonElement>> GetObjectAsList(this JsonElement that)
    {
        if (!that.TryGetObjectAsList(out List<KeyValuePair<string, JsonElement>>? value))
            throw new InvalidOperationException("This value's ValueKind is not Object.");

        return value;
    }

    public static bool TryGetObjectAsList(this JsonElement that, [MaybeNullWhen(returnValue: false)] out List<KeyValuePair<string, JsonElement>> value)
    {
        if (that.ValueKind != JsonValueKind.Object)
        {
            value = null;
            return false;
        } // if (...)

        value = new();
        using JsonElement.ObjectEnumerator enumerator = that.EnumerateObject();

        while (enumerator.MoveNext())
        {
            JsonProperty property = enumerator.Current;
            value.Add(new(property.Name, property.Value));
        } // while (...)

        value.Sort((a, b) => a.Key.CompareTo(b.Key));
        return true;
    }

    public static bool IsEquivalent(this JsonElement that, JsonElement other)
    {
        if (that.ValueKind != other.ValueKind)
            return false;

        switch (that.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return true;
            case JsonValueKind.String:
                return that.GetString() == other.GetString();
            case JsonValueKind.Number:
                if (that.TryGetInt64(out long integerValue1))
                {
                    if (other.TryGetInt64(out long integerValue2))
                        return integerValue1 == integerValue2;
                    else
                        return false;
                } // if (...)
                else if (that.TryGetDecimal(out decimal decimalValue1))
                {
                    if (other.TryGetDecimal(out decimal decimalValue2))
                        return decimalValue1 == decimalValue2;
                    else
                        return false;
                } // else if (...)
                else if (that.TryGetDouble(out double doubleValue1))
                {
                    if (other.TryGetDouble(out double doubleValue2))
                        return doubleValue1 == doubleValue2;
                    else
                        return false;
                } // else if (...)
                else
                    return that.GetRawText() == other.GetRawText();
            case JsonValueKind.Array:
                List<JsonElement> list1 = that.GetArrayAsList();
                List<JsonElement> list2 = other.GetArrayAsList();

                if (list1.Count != list2.Count)
                    return false;

                for (int i = 0; i < list1.Count; ++i)
                    if (!list1[i].IsEquivalent(list2[i]))
                        return false;

                return true;
            case JsonValueKind.Object:
                List<KeyValuePair<string, JsonElement>> map1 = that.GetObjectAsList();
                List<KeyValuePair<string, JsonElement>> map2 = other.GetObjectAsList();

                if (map1.Count != map2.Count)
                    return false;

                for (int i = 0; i < map1.Count; ++i)
                {
                    KeyValuePair<string, JsonElement> pair1 = map1[i];
                    KeyValuePair<string, JsonElement> pair2 = map2[i];

                    if (pair1.Key != pair2.Key)
                        return false;

                    if (!pair1.Value.IsEquivalent(pair2.Value))
                        return false;
                } // for (...)

                return true;
            default:
                return false;
        }
    }

    public static void FastForwardToEndArray(this ref Utf8JsonReader json)
        => json.FastForwardTo(JsonTokenType.EndArray);

    public static void FastForwardToEndObject(this ref Utf8JsonReader json)
        => json.FastForwardTo(JsonTokenType.EndObject);

    private static void FastForwardTo(this ref Utf8JsonReader json, JsonTokenType endToken)
    {
        while (json.Read() && json.TokenType != endToken)
            json.Skip();
    }
}
