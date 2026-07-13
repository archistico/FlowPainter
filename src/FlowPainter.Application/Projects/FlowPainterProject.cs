using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.Detail;

namespace FlowPainter.Application.Projects;

public sealed class FlowPainterProject
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;

    public FlowPainterProject(
        string name,
        string sourcePath,
        ulong seed,
        FlowPainterSettings settings,
        PreviewSettings? preview = null,
        IReadOnlyList<DetailRegion>? detailRegions = null,
        FinalRenderSettings? finalRender = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A project must have a non-empty name.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("A project must reference a source image.", nameof(sourcePath));
        }

        ArgumentNullException.ThrowIfNull(settings);

        Name = name.Trim();
        SourcePath = sourcePath.Trim();
        Seed = seed;
        Settings = settings;
        Preview = preview ?? new PreviewSettings();
        FinalRender = finalRender ?? new FinalRenderSettings();
        DetailRegion[] copiedRegions = detailRegions?.ToArray() ?? [];

        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (DetailRegion region in copiedRegions)
        {
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Duplicate detail-region identifier '{region.Id}'.",
                    nameof(detailRegions));
            }
        }

        _detailRegions = Array.AsReadOnly(copiedRegions);
    }

    public string Name { get; }

    public string SourcePath { get; }

    public ulong Seed { get; }

    public FlowPainterSettings Settings { get; }

    public PreviewSettings Preview { get; }

    public FinalRenderSettings FinalRender { get; }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;
}
