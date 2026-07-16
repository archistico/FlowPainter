namespace FlowPainter.Domain.Segmentation;

public readonly record struct PixelBounds
{
    public PixelBounds(int left, int top, int right, int bottom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);

        if (right <= left)
        {
            throw new ArgumentException("The right edge must be greater than the left edge.", nameof(right));
        }

        if (bottom <= top)
        {
            throw new ArgumentException("The bottom edge must be greater than the top edge.", nameof(bottom));
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left { get; }

    public int Top { get; }

    public int Right { get; }

    public int Bottom { get; }

    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool Contains(int x, int y)
    {
        return x >= Left && x < Right && y >= Top && y < Bottom;
    }
}
