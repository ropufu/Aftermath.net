namespace Ropufu;

public static class Combinatorics
{
    /// <summary>
    /// Constructs the sequence (0, 1, ..., n - 1).
    /// </summary>
    public static int[] TrivialPermutation(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        int[] result = new int[n];

        for (int i = 0; i < n; ++i)
            result[i] = i;

        return result;
    }

    /// <summary>
    /// Constructs the sequence (n - 1, n - 2, ..., 0).
    /// </summary>
    public static int[] ReversePermutation(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        int[] result = new int[n];

        for (int i = 0; i < n; ++i)
            result[i] = n - 1 - i;

        return result;
    }

    /// <summary>
    /// Calculates the number of ways to select a subset of size <paramref name="k"/> from a set of size <paramref name="n"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OverflowException"></exception>
    public static int BinomialCoefficient(int n, int k)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        if (k < 0 || k > n)
            throw new ArgumentOutOfRangeException(nameof(k));

        if (k == 0 || k == n)
            return 1;

        if (k + k > n)
            return Combinatorics.BinomialCoefficient(n, n - k);

        int result = n - k + 1;
        int numerator = result + 1;

        //for (int denominator = 2; denominator <= k; ++denominator, ++numerator)
        //    result = checked(result * numerator) / denominator;

        for (int denominator = 2; denominator <= k; ++denominator, ++numerator)
            result = Combinatorics.MultiplyThenDivide(result, numerator, denominator);

        return result;
    }

    /// <summary>
    /// Calculates (<paramref name="a"/> · <paramref name="b"/> / <paramref name="denominator"/>)
    /// avoiding potential overflow resulting from the multiplication.
    /// </summary>
    /// <exception cref="OverflowException"></exception>
    /// <remarks>
    /// The product <paramref name="a"/> · <paramref name="b"/> should be divisible by <paramref name="denominator"/>.
    /// </remarks>
    private static int MultiplyThenDivide(int a, int b, int denominator)
    {
        int x = Combinatorics.GreatestCommonDivisor(a, denominator);
        a /= x;
        denominator /= x;

        x = Combinatorics.GreatestCommonDivisor(b, denominator);
        b /= x;
        //// By now (denominator / x) should equal 1.
        //if ((denominator / x) != 1)
        //    throw new ApplicationException();

        return checked(a * b);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        } // while (...)

        return (a | b);
    }
}
