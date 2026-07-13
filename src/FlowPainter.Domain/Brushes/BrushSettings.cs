namespace FlowPainter.Domain.Brushes;

public sealed class BrushSettings
{
    public const double DefaultHardness = 0.8d;
    public const double DefaultSizeJitter = 0d;
    public const double DefaultOpacityJitter = 0d;
    public const int DefaultBristleCount = 7;
    public const double DefaultBristleSpread = 0.75d;
    public const int MaximumBristleCount = 64;

    public BrushSettings(
        BrushKind kind = BrushKind.SolidRound,
        double hardness = DefaultHardness,
        double sizeJitter = DefaultSizeJitter,
        double opacityJitter = DefaultOpacityJitter,
        int bristleCount = DefaultBristleCount,
        double bristleSpread = DefaultBristleSpread)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown brush kind.");
        }

        ValidateUnitInterval(hardness, nameof(hardness));
        ValidateUnitInterval(sizeJitter, nameof(sizeJitter));
        ValidateUnitInterval(opacityJitter, nameof(opacityJitter));
        ArgumentOutOfRangeException.ThrowIfLessThan(bristleCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bristleCount, MaximumBristleCount);
        ValidateUnitInterval(bristleSpread, nameof(bristleSpread));

        Kind = kind;
        Hardness = hardness;
        SizeJitter = sizeJitter;
        OpacityJitter = opacityJitter;
        BristleCount = bristleCount;
        BristleSpread = bristleSpread;
    }

    public BrushKind Kind { get; }

    public double Hardness { get; }

    public double SizeJitter { get; }

    public double OpacityJitter { get; }

    public int BristleCount { get; }

    public double BristleSpread { get; }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and in the [0, 1] range.");
        }
    }
}
