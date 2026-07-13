namespace FlowPainter.Domain.Images;

public readonly record struct ImageSize
{
    public const int MaximumDimension = 10_000;
    public const int RgbaBytesPerPixel = 4;

    public ImageSize(int width, int height)
    {
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public long PixelCount => checked((long)Width * Height);

    public double AspectRatio => (double)Width / Height;

    public long GetRequiredBytes(int bytesPerPixel)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerPixel);
        return checked(PixelCount * bytesPerPixel);
    }

    public ImageSize FitWithin(int maximumWidth, int maximumHeight, bool allowUpscale = false)
    {
        ValidateDimension(maximumWidth, nameof(maximumWidth));
        ValidateDimension(maximumHeight, nameof(maximumHeight));

        double widthScale = (double)maximumWidth / Width;
        double heightScale = (double)maximumHeight / Height;
        double scale = Math.Min(widthScale, heightScale);

        if (!allowUpscale && scale >= 1d)
        {
            return this;
        }

        int fittedWidth = Math.Max(1, (int)Math.Floor(Width * scale));
        int fittedHeight = Math.Max(1, (int)Math.Floor(Height * scale));

        return new ImageSize(fittedWidth, fittedHeight);
    }

    private static void ValidateDimension(int value, string parameterName)
    {
        if (value <= 0 || value > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Image dimensions must be between 1 and {MaximumDimension:N0} pixels.");
        }
    }
}
