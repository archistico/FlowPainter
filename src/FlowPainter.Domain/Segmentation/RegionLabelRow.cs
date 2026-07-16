namespace FlowPainter.Domain.Segmentation;

public readonly ref struct RegionLabelRow
{
    private readonly ReadOnlySpan<ushort> _labels16;
    private readonly ReadOnlySpan<uint> _labels32;

    internal RegionLabelRow(ReadOnlySpan<ushort> labels)
    {
        _labels16 = labels;
        _labels32 = default;
    }

    internal RegionLabelRow(ReadOnlySpan<uint> labels)
    {
        _labels16 = default;
        _labels32 = labels;
    }

    public int Length => _labels16.IsEmpty ? _labels32.Length : _labels16.Length;

    public uint this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);

            return _labels16.IsEmpty ? _labels32[index] : _labels16[index];
        }
    }

    public void CopyTo(Span<uint> destination)
    {
        if (destination.Length < Length)
        {
            throw new ArgumentException("The destination span is smaller than the label row.", nameof(destination));
        }

        for (int index = 0; index < Length; index++)
        {
            destination[index] = this[index];
        }
    }
}
