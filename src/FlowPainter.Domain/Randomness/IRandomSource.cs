namespace FlowPainter.Domain.Randomness;

public interface IRandomSource
{
    ulong NextUInt64();

    double NextDouble();

    int NextInt32(int maximumExclusive);
}
