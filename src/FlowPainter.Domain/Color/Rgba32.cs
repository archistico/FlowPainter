namespace FlowPainter.Domain.Color;

public readonly record struct Rgba32(byte Red, byte Green, byte Blue, byte Alpha)
{
    public static Rgba32 Opaque(byte red, byte green, byte blue)
    {
        return new Rgba32(red, green, blue, byte.MaxValue);
    }

    public uint ToRgbaUInt32()
    {
        return ((uint)Red << 24)
            | ((uint)Green << 16)
            | ((uint)Blue << 8)
            | Alpha;
    }

    public static Rgba32 FromRgbaUInt32(uint value)
    {
        return new Rgba32(
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value);
    }
}
