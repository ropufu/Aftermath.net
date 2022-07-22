using Xunit;

namespace Ropufu.Tests;

public class UnorderedSampleWithoutReplacementTest
{
    [Fact]
    public void TenChooseFive()
    {
        const int n = 10;
        const int k = 5;

        UnorderedSampleWithoutReplacement.Enumerator x = new(n, k);
        int expectedCount = Combinatorics.BinomialCoefficient(n, k);

        int count = 0;

        while (x.MoveNext())
            ++count;

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void TwentyChooseFive()
    {
        const int n = 20;
        const int k = 5;

        UnorderedSampleWithoutReplacement.Enumerator x = new(n, k);
        int expectedCount = Combinatorics.BinomialCoefficient(n, k);

        int count = 0;

        while (x.MoveNext())
            ++count;

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void TwentyChooseTwelve()
    {
        const int n = 20;
        const int k = 12;

        UnorderedSampleWithoutReplacement.Enumerator x = new(n, k);
        int expectedCount = Combinatorics.BinomialCoefficient(n, k);

        int count = 0;

        while (x.MoveNext())
            ++count;

        Assert.Equal(expectedCount, count);
    }
}
