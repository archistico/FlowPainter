namespace FlowPainter.Application.FlowPainting.Legacy;

public sealed class TrigonometricLegacyScalarFieldFactory : ILegacyScalarFieldFactory
{
    public ILegacyScalarField Create(int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        return new TrigonometricLegacyScalarField(seed);
    }

    private sealed class TrigonometricLegacyScalarField : ILegacyScalarField
    {
        private readonly double _phaseX;
        private readonly double _phaseY;

        public TrigonometricLegacyScalarField(int seed)
        {
            _phaseX = (seed % 10_007) / 10_007d * Math.Tau;
            _phaseY = (seed % 7_919) / 7_919d * Math.Tau;
        }

        public double Sample(double x, double y)
        {
            if (!double.IsFinite(x) || !double.IsFinite(y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Field coordinates must be finite.");
            }

            double firstWave = Math.Sin((x * 4.1d) + _phaseX);
            double secondWave = Math.Cos((y * 3.7d) + _phaseY);
            double crossingWave = Math.Sin(((x + y) * 2.3d) + (_phaseX * 0.5d));
            double normalized = 0.5d + (firstWave * 0.2d) + (secondWave * 0.2d) + (crossingWave * 0.1d);

            return Math.Clamp(normalized, 0d, 1d);
        }
    }
}
