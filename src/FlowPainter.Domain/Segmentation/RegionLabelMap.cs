using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Segmentation;

public sealed class RegionLabelMap
{
    public const int MaximumUInt16RegionCount = ushort.MaxValue + 1;

    private readonly ushort[]? _labels16;
    private readonly uint[]? _labels32;

    private RegionLabelMap(
        ImageSize size,
        int regionCount,
        ushort[]? labels16,
        uint[]? labels32)
    {
        Size = size;
        RegionCount = regionCount;
        _labels16 = labels16;
        _labels32 = labels32;
    }

    public ImageSize Size { get; }

    public int RegionCount { get; }

    public RegionLabelStorageKind StorageKind => _labels16 is null
        ? RegionLabelStorageKind.Wide
        : RegionLabelStorageKind.Compact;

    public long RequiredBytes => GetRequiredBytes(Size, RegionCount);

    public uint this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Size.Width);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

            int index = checked((y * Size.Width) + x);
            return _labels16 is null ? _labels32![index] : _labels16[index];
        }
    }

    public static RegionLabelMap Create(
        ImageSize size,
        int regionCount,
        ReadOnlySpan<uint> labels)
    {
        ValidateRegionCount(size, regionCount);

        if (labels.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} labels but received {labels.Length}.",
                nameof(labels));
        }

        bool[] usedLabels = new bool[regionCount];
        RegionLabelStorageKind storageKind = SelectStorage(regionCount);

        if (storageKind == RegionLabelStorageKind.Compact)
        {
            ushort[] compactLabels = new ushort[labels.Length];
            for (int index = 0; index < labels.Length; index++)
            {
                uint label = labels[index];
                ValidateLabel(label, regionCount, nameof(labels));
                compactLabels[index] = checked((ushort)label);
                usedLabels[checked((int)label)] = true;
            }

            ValidateAllLabelsUsed(usedLabels, nameof(labels));
            return new RegionLabelMap(size, regionCount, compactLabels, null);
        }

        uint[] wideLabels = labels.ToArray();
        for (int index = 0; index < wideLabels.Length; index++)
        {
            uint label = wideLabels[index];
            ValidateLabel(label, regionCount, nameof(labels));
            usedLabels[checked((int)label)] = true;
        }

        ValidateAllLabelsUsed(usedLabels, nameof(labels));
        return new RegionLabelMap(size, regionCount, null, wideLabels);
    }

    public static RegionLabelMap Create(
        ImageSize size,
        int regionCount,
        ReadOnlySpan<int> labels)
    {
        ValidateRegionCount(size, regionCount);

        if (labels.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} labels but received {labels.Length}.",
                nameof(labels));
        }

        bool[] usedLabels = new bool[regionCount];
        RegionLabelStorageKind storageKind = SelectStorage(regionCount);

        if (storageKind == RegionLabelStorageKind.Compact)
        {
            ushort[] compactLabels = new ushort[labels.Length];
            for (int index = 0; index < labels.Length; index++)
            {
                int label = labels[index];
                ValidateLabel(label, regionCount, nameof(labels));
                compactLabels[index] = checked((ushort)label);
                usedLabels[label] = true;
            }

            ValidateAllLabelsUsed(usedLabels, nameof(labels));
            return new RegionLabelMap(size, regionCount, compactLabels, null);
        }

        uint[] wideLabels = new uint[labels.Length];
        for (int index = 0; index < labels.Length; index++)
        {
            int label = labels[index];
            ValidateLabel(label, regionCount, nameof(labels));
            wideLabels[index] = checked((uint)label);
            usedLabels[label] = true;
        }

        ValidateAllLabelsUsed(usedLabels, nameof(labels));
        return new RegionLabelMap(size, regionCount, null, wideLabels);
    }

    public static RegionLabelStorageKind SelectStorage(int regionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(regionCount);
        return regionCount <= MaximumUInt16RegionCount
            ? RegionLabelStorageKind.Compact
            : RegionLabelStorageKind.Wide;
    }

    public static long GetRequiredBytes(ImageSize size, int regionCount)
    {
        ValidateRegionCount(size, regionCount);
        int bytesPerLabel = SelectStorage(regionCount) == RegionLabelStorageKind.Compact
            ? sizeof(ushort)
            : sizeof(uint);
        return size.GetRequiredBytes(bytesPerLabel);
    }

    public RegionLabelRow GetRow(int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

        int start = checked(y * Size.Width);
        return _labels16 is null
            ? new RegionLabelRow(_labels32!.AsSpan(start, Size.Width))
            : new RegionLabelRow(_labels16.AsSpan(start, Size.Width));
    }

    public uint[] CopyLabels()
    {
        uint[] copy = new uint[checked((int)Size.PixelCount)];
        for (int y = 0; y < Size.Height; y++)
        {
            GetRow(y).CopyTo(copy.AsSpan(y * Size.Width, Size.Width));
        }

        return copy;
    }

    public int[] CountPixelsByRegion()
    {
        int[] counts = new int[RegionCount];
        for (int y = 0; y < Size.Height; y++)
        {
            RegionLabelRow row = GetRow(y);
            for (int x = 0; x < row.Length; x++)
            {
                counts[checked((int)row[x])]++;
            }
        }

        return counts;
    }

    private static void ValidateRegionCount(ImageSize size, int regionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(regionCount);
        if (regionCount > size.PixelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(regionCount),
                regionCount,
                "The number of regions cannot exceed the number of pixels.");
        }
    }

    private static void ValidateLabel(uint label, int regionCount, string parameterName)
    {
        if (label >= regionCount)
        {
            throw new ArgumentException(
                $"Label {label} is outside the compact range 0 to {regionCount - 1}.",
                parameterName);
        }
    }

    private static void ValidateLabel(int label, int regionCount, string parameterName)
    {
        if (label < 0 || label >= regionCount)
        {
            throw new ArgumentException(
                $"Label {label} is outside the compact range 0 to {regionCount - 1}.",
                parameterName);
        }
    }

    private static void ValidateAllLabelsUsed(bool[] usedLabels, string parameterName)
    {
        for (int label = 0; label < usedLabels.Length; label++)
        {
            if (!usedLabels[label])
            {
                throw new ArgumentException(
                    $"Compact label {label} is not assigned to any pixel.",
                    parameterName);
            }
        }
    }
}
