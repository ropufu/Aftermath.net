using Xunit;

namespace Ropufu.Json.Tests;

public class JsonPointerTest
{
    [Fact]
    public void InvalidUnescape()
    {
        Assert.False(JsonPointer.TryUnescape("a/b"));
        Assert.False(JsonPointer.TryUnescape("m~n"));
    }

    [Fact]
    public void InvalidParsing()
    {
        Assert.False(JsonPointer.TryParse("a/b", out _));
        Assert.False(JsonPointer.TryParse("/m~n", out _));
    }

    [Fact]
    public void ValidUnescape()
    {
        Assert.True(JsonPointer.TryUnescape("a~1b", out string? x));
        Assert.True(JsonPointer.TryUnescape("m~0n", out string? y));
        Assert.True(JsonPointer.TryUnescape("foo", out string? z));
        Assert.True(JsonPointer.TryUnescape("", out string? w));

        Assert.Equal("a/b", x);
        Assert.Equal("m~n", y);
        Assert.Equal("foo", z);
        Assert.Equal("", w);
    }

    [Fact]
    public void ValidParsingZero()
    {
        Assert.True(JsonPointer.TryParse("", out JsonPointer? w));

        Assert.Equal(0, w!.Length);
    }

    [Fact]
    public void ValidParsingOne()
    {
        Assert.True(JsonPointer.TryParse("/a~1b", out JsonPointer? x));
        Assert.True(JsonPointer.TryParse("/m~0n", out JsonPointer? y));
        Assert.True(JsonPointer.TryParse("/foo", out JsonPointer? z));

        Assert.Equal(1, x!.Length);
        Assert.Equal(1, y!.Length);
        Assert.Equal(1, z!.Length);
    }

    [Fact]
    public void ValidParsingTwo()
    {
        Assert.True(JsonPointer.TryParse("/a/b", out JsonPointer? x));

        Assert.Equal(2, x!.Length);
    }
}
