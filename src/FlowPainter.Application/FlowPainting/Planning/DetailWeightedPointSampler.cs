using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.FlowPainting.Planning;

internal sealed class DetailWeightedPointSampler
{
    private readonly DetailMap _map;
    private readonly double[] _cumulativeWeights;
    private readonly double _totalWeight;

    public DetailWeightedPointSampler(
        DetailMap map,
        DetailInfluenceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(settings);

        _map = map;
        float[] values = map.CopyValues();
        _cumulativeWeights = new double[values.Length];
        double total = 0d;

        for (int index = 0; index < values.Length; index++)
        {
            total += settings.GetPlacementWeight(values[index]);
            _cumulativeWeights[index] = total;
        }

        _totalWeight = total;
    }

    public NormalizedPoint Sample(DeterministicRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        double target = random.NextDouble() * _totalWeight;
        int index = Array.BinarySearch(_cumulativeWeights, target);
        if (index < 0)
        {
            index = ~index;
        }

        index = Math.Min(index, _cumulativeWeights.Length - 1);
        int x = index % _map.Width;
        int y = index / _map.Width;
        double normalizedX = (x + random.NextDouble()) / _map.Width;
        double normalizedY = (y + random.NextDouble()) / _map.Height;
        return new NormalizedPoint(
            Math.Min(normalizedX, Math.BitDecrement(1d)),
            Math.Min(normalizedY, Math.BitDecrement(1d)));
    }
}
