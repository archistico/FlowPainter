using FlowPainter.Domain.FlowFields;

namespace FlowPainter.Application.FlowPainting.Fields;

public sealed class FlowFieldSettings
{
    public const double DefaultScale = 3.5d;
    public const int DefaultOctaves = 4;
    public const double DefaultPersistence = 0.55d;
    public const double DefaultLacunarity = 2d;
    public const double DefaultAngleOffsetRadians = 0d;

    public FlowFieldSettings(
        FlowFieldKind kind = FlowFieldKind.CoherentNoise,
        double scale = DefaultScale,
        int octaves = DefaultOctaves,
        double persistence = DefaultPersistence,
        double lacunarity = DefaultLacunarity,
        double angleOffsetRadians = DefaultAngleOffsetRadians)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown flow-field kind.");
        }

        ValidateFinitePositive(scale, nameof(scale));
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(octaves, 8);

        if (!double.IsFinite(persistence) || persistence <= 0d || persistence > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(persistence),
                persistence,
                "Persistence must be finite and in the (0, 1] range.");
        }

        if (!double.IsFinite(lacunarity) || lacunarity < 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lacunarity),
                lacunarity,
                "Lacunarity must be finite and greater than or equal to one.");
        }

        if (!double.IsFinite(angleOffsetRadians))
        {
            throw new ArgumentOutOfRangeException(
                nameof(angleOffsetRadians),
                angleOffsetRadians,
                "The angle offset must be finite.");
        }

        Kind = kind;
        Scale = scale;
        Octaves = octaves;
        Persistence = persistence;
        Lacunarity = lacunarity;
        AngleOffsetRadians = angleOffsetRadians;
    }

    public FlowFieldKind Kind { get; }

    public double Scale { get; }

    public int Octaves { get; }

    public double Persistence { get; }

    public double Lacunarity { get; }

    public double AngleOffsetRadians { get; }

    private static void ValidateFinitePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and greater than zero.");
        }
    }
}
