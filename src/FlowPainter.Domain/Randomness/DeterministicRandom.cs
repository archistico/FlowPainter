namespace FlowPainter.Domain.Randomness;

public sealed class DeterministicRandom : IRandomSource
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
    private ulong _state;

    public DeterministicRandom(ulong seed)
    {
        _state = seed;
    }

    public ulong NextUInt64()
    {
        unchecked
        {
            ulong value = _state += GoldenGamma;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    public double NextDouble()
    {
        return (NextUInt64() >> 11) * (1d / (1UL << 53));
    }

    public int NextInt32(int maximumExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumExclusive);

        ulong range = (ulong)maximumExclusive;
        ulong rejectionLimit = ulong.MaxValue - (ulong.MaxValue % range);
        ulong value;

        do
        {
            value = NextUInt64();
        }
        while (value >= rejectionLimit);

        return (int)(value % range);
    }
}
