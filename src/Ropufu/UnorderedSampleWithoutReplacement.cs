using System.Collections;

namespace Ropufu;

public static class UnorderedSampleWithoutReplacement
{
    /// <summary>
    /// Enumerates all unordered samples of a given size (zero-based indices) from a population of a given size.
    /// </summary>
    public sealed class Enumerator : IEnumerator<IList<int>>
    {
        private bool _disposedValue;
        private readonly int _n;
        private readonly int _k;
        private int[] _indices;

        /// <param name="n">Population size.</param>
        /// <param name="k">Sample size.</param>
        /// <exception cref="ArgumentOutOfRangeException">Population size must be nonnegative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Sample size must be between 0 and <paramref name="n"/>.</exception>
        public Enumerator(int n, int k)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n));

            if (k < 0 || k > n)
                throw new ArgumentOutOfRangeException(nameof(k));

            _n = n;
            _k = k;
            _indices = new int[k];

            this.Reset();
        }

        public int PopulationSize
            => _n;

        public int SampleSize
            => _k;

        public IList<int> Current
            => Array.AsReadOnly(_indices);

        object IEnumerator.Current
            => this.Current;

        public bool MoveNext()
        {
            int position = _k;
            int threshold = _n;

            // Terminal combination is (n - k), ... (n - 1).
            // Traverse current sequence right-to-left to find the last (rightmost) position
            // where the combination differs from the terminal one.
            while (position > 0)
            {
                --position;
                --threshold;

                // Check if the highest possible value at the position has been reached.
                if (_indices[position] != threshold)
                {
                    // Update indices left-to-right from the current position to the end.
                    for (threshold = _indices[position]; position < _k; ++position)
                        _indices[position] = ++threshold;

                    return true;
                } // if (...)
            } // while (...)

            return false;
        }

        public void Reset()
        {
            for (int i = 0; i < _k; ++i)
                _indices[i] = i;

            if (_k != 0)
                --_indices[_k - 1];
        }

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
                _indices = null!;

            _disposedValue = true;
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
