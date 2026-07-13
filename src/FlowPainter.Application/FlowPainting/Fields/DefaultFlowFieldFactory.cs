using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.FlowPainting.Fields;

public sealed class DefaultFlowFieldFactory : IFlowFieldFactory
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The factory is an injected application service and must retain instance semantics for replacement and composition.")]
    public IFlowField Create(int seed, FlowFieldSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Kind switch
        {
            FlowFieldKind.CoherentNoise => new CoherentNoiseFlowField(seed, settings),
            FlowFieldKind.LegacyTrigonometric => new TrigonometricFlowField(seed, settings),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Kind, "Unknown flow-field kind.")
        };
    }

    private sealed class CoherentNoiseFlowField : IFlowField
    {
        private const double UnitDoubleScale = 1d / (1UL << 53);
        private readonly ulong _seed;
        private readonly FlowFieldSettings _settings;

        public CoherentNoiseFlowField(int seed, FlowFieldSettings settings)
        {
            _seed = unchecked((ulong)seed);
            _settings = settings;
        }

        public double SampleAngle(double x, double y)
        {
            ValidateCoordinates(x, y);

            double frequency = _settings.Scale;
            double amplitude = 1d;
            double weightedValue = 0d;
            double amplitudeTotal = 0d;

            for (int octave = 0; octave < _settings.Octaves; octave++)
            {
                weightedValue += SampleValueNoise(x * frequency, y * frequency, octave) * amplitude;
                amplitudeTotal += amplitude;
                frequency *= _settings.Lacunarity;
                amplitude *= _settings.Persistence;
            }

            double normalized = 0.5d + (0.5d * weightedValue / amplitudeTotal);
            double angle = (Math.Clamp(normalized, 0d, 1d) * AngleMath.Tau)
                + _settings.AngleOffsetRadians;

            return AngleMath.NormalizeRadians(angle);
        }

        private double SampleValueNoise(double x, double y, int octave)
        {
            long x0 = checked((long)Math.Floor(x));
            long y0 = checked((long)Math.Floor(y));
            long x1 = checked(x0 + 1L);
            long y1 = checked(y0 + 1L);
            double tx = Smooth(x - x0);
            double ty = Smooth(y - y0);

            double top = Lerp(
                LatticeValue(x0, y0, octave),
                LatticeValue(x1, y0, octave),
                tx);
            double bottom = Lerp(
                LatticeValue(x0, y1, octave),
                LatticeValue(x1, y1, octave),
                tx);

            return Lerp(top, bottom, ty);
        }

        private double LatticeValue(long x, long y, int octave)
        {
            ulong hash = _seed;
            hash ^= unchecked((ulong)x) * 0x9E37_79B9_7F4A_7C15UL;
            hash ^= unchecked((ulong)y) * 0xBF58_476D_1CE4_E5B9UL;
            hash ^= unchecked((ulong)octave) * 0x94D0_49BB_1331_11EBUL;
            hash = Mix(hash);
            double unit = (hash >> 11) * UnitDoubleScale;
            return (unit * 2d) - 1d;
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9E37_79B9_7F4A_7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58_476D_1CE4_E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D0_49BB_1331_11EBUL;
            return value ^ (value >> 31);
        }

        private static double Smooth(double value)
        {
            return value * value * (3d - (2d * value));
        }

        private static double Lerp(double start, double end, double amount)
        {
            return start + ((end - start) * amount);
        }
    }

    private sealed class TrigonometricFlowField : IFlowField
    {
        private readonly double _phaseX;
        private readonly double _phaseY;
        private readonly FlowFieldSettings _settings;

        public TrigonometricFlowField(int seed, FlowFieldSettings settings)
        {
            _phaseX = (seed % 10_007) / 10_007d * AngleMath.Tau;
            _phaseY = (seed % 7_919) / 7_919d * AngleMath.Tau;
            _settings = settings;
        }

        public double SampleAngle(double x, double y)
        {
            ValidateCoordinates(x, y);

            double scaledX = x * _settings.Scale;
            double scaledY = y * _settings.Scale;
            double firstWave = Math.Sin((scaledX * 4.1d) + _phaseX);
            double secondWave = Math.Cos((scaledY * 3.7d) + _phaseY);
            double crossingWave = Math.Sin(((scaledX + scaledY) * 2.3d) + (_phaseX * 0.5d));
            double normalized = Math.Clamp(
                0.5d + (firstWave * 0.2d) + (secondWave * 0.2d) + (crossingWave * 0.1d),
                0d,
                1d);

            return AngleMath.NormalizeRadians(
                (normalized * AngleMath.Tau) + _settings.AngleOffsetRadians);
        }
    }

    private static void ValidateCoordinates(double x, double y)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Field coordinates must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Field coordinates must be finite.");
        }
    }
}
