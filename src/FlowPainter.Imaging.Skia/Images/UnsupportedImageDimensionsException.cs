namespace FlowPainter.Imaging.Skia.Images;

public sealed class UnsupportedImageDimensionsException : IOException
{
    public UnsupportedImageDimensionsException()
        : this("The image dimensions exceed the supported limit.")
    {
    }

    public UnsupportedImageDimensionsException(string? message)
        : base(message)
    {
    }

    public UnsupportedImageDimensionsException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public UnsupportedImageDimensionsException(int width, int height, int maximumDimension)
        : base($"Image dimensions {width:N0} × {height:N0} exceed the supported limit of {maximumDimension:N0} pixels per side.")
    {
        Width = width;
        Height = height;
        MaximumDimension = maximumDimension;
    }

    public int Width { get; }

    public int Height { get; }

    public int MaximumDimension { get; }
}
