using System;
using Xunit;

namespace Ropufu.Tests;

public class CombinatoricsTest
{
    [Fact]
    public void BinomialCoefficientChooseZero()
    {
        for (int n = 1; n < 10; ++n)
            Assert.Equal(1, Combinatorics.BinomialCoefficient(n, 0));
    }

    [Fact]
    public void BinomialCoefficientChooseAll()
    {
        for (int n = 1; n < 10; ++n)
            Assert.Equal(1, Combinatorics.BinomialCoefficient(n, n));
    }

    [Fact]
    public void BinomialCoefficientLargeValues()
    {
        Assert.Equal(1166803110, Combinatorics.BinomialCoefficient(33, 17));
        Assert.Equal(1917334783, Combinatorics.BinomialCoefficient(43, 10));
        Assert.Equal(2054455634, Combinatorics.BinomialCoefficient(49, 9));
    }

    [Fact]
    public void BinomialCoefficientOverflow()
    {
        // (34 choose 17) = 2333606220 > int.MaxValue = 2147483647.
        Assert.Throws<OverflowException>(() => Combinatorics.BinomialCoefficient(34, 17));
    }
}
