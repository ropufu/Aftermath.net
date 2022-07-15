using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Ropufu.Json.Tests;

public class NoexceptJsonTest
{
    private static Utf8JsonParser<string> s_notNullParser;
    private static Utf8JsonParser<string?> s_maybeNullParser;

    static NoexceptJsonTest()
    {
        NullabilityAwareType<string> notNullType = NullabilityAwareType.MakeSimple<string>(NullabilityState.NotNull);
        NullabilityAwareType<string> maybeNullType = NullabilityAwareType.MakeSimple<string>(NullabilityState.Nullable);

        NoexceptJson.TryMakeParser(notNullType, out s_notNullParser!);
        NoexceptJson.TryMakeParser(maybeNullType, out s_maybeNullParser!);
    }

    [Fact]
    public void EscapedSixCharacterString()
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes("\"\\u005C\"");
        Utf8JsonReader reader = new(utf8Bytes);
        reader.Read();

        string? value;

        s_notNullParser(ref reader, out value);
        Assert.Equal("\\", value);

        s_maybeNullParser(ref reader, out value);
        Assert.Equal("\\", value);
    }

    [Fact]
    public void EscapedTwoCharacterString()
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(@"""\\""");
        Utf8JsonReader reader = new(utf8Bytes);
        reader.Read();

        string? value;

        s_notNullParser(ref reader, out value);
        Assert.Equal("\\", value);

        s_maybeNullParser(ref reader, out value);
        Assert.Equal("\\", value);
    }

    [Fact]
    public void MalformedArray()
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(@"[""first"", 2, ""last""]");
        Utf8JsonReader reader = new(utf8Bytes);
        reader.Read();

        NullabilityAwareType<string> stringType = NullabilityAwareType.MakeSimple<string>(NullabilityState.NotNull);
        NullabilityAwareType<string[]> arrayType = stringType.MakeArrayType(NullabilityState.NotNull);


        Assert.True(NoexceptJson.TryMakeParser(arrayType, out Utf8JsonParser<string[]>? arrayParser));

        Assert.False(arrayParser!(ref reader, out _));

        Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
    }
}
