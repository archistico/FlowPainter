using FlowPainter.Application.Background;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.FlowPainting.Planning;

internal sealed class ArtisticDetailPointSampler
{
    private readonly ArtisticDetailField _field;
    private readonly double[] _cumulativeWeights;
    private readonly double _totalWeight;

    public ArtisticDetailPointSampler(
        ArtisticDetailField field,
        DetailInfluenceSettings detailSettings,
        BackgroundSuppressionSettings backgroundSettings)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(detailSettings);
        ArgumentNullException.ThrowIfNull(backgroundSettings);

        _field = field;
        float[] values = field.CopyValues();
        _cumulativeWeights = new double[values.Length];
        double total = 0d;
        for (int index = 0; index < values.Length; index++)
        {
            double value = values[index];
            double weight = value >= 0d
                ? detailSettings.GetPlacementWeight(value)
                : backgroundSettings.GetPlacementMultiplier(-value);
            total += weight;
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
        int x = index % _field.Width;
        int y = index / _field.Width;
        double normalizedX = (x + random.NextDouble()) / _field.Width;
        double normalizedY = (y + random.NextDouble()) / _field.Height;
        return new NormalizedPoint(
            Math.Min(normalizedX, Math.BitDecrement(1d)),
            Math.Min(normalizedY, Math.BitDecrement(1d)));
    }
}
