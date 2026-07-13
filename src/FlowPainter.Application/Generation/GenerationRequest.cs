using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Generation;

public sealed class GenerationRequest
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;

    public GenerationRequest(
        ImageSize sourceSize,
        ImageSize outputSize,
        ulong seed,
        GenerativeMode mode,
        IEnumerable<DetailRegion>? detailRegions = null)
    {
        SourceSize = sourceSize;
        OutputSize = outputSize;
        Seed = seed;
        Mode = mode;

        DetailRegion[] regions = detailRegions?.ToArray() ?? [];
        ValidateUniqueRegionIds(regions);
        _detailRegions = Array.AsReadOnly(regions);
    }

    public ImageSize SourceSize { get; }

    public ImageSize OutputSize { get; }

    public ulong Seed { get; }

    public GenerativeMode Mode { get; }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;

    private static void ValidateUniqueRegionIds(IEnumerable<DetailRegion> regions)
    {
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);

        foreach (DetailRegion region in regions)
        {
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Detail region identifiers must be unique. Duplicate: '{region.Id}'.",
                    nameof(regions));
            }
        }
    }
}
