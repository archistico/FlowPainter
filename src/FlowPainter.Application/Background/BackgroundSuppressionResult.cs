using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Background;

public sealed class BackgroundSuppressionResult
{
    public BackgroundSuppressionResult(
        ArtisticDetailField artisticDetailField,
        DetailMap suppressionMap,
        DetailMap protectionMap,
        DetailMap effectiveDetailMap)
    {
        ArgumentNullException.ThrowIfNull(artisticDetailField);
        ArgumentNullException.ThrowIfNull(suppressionMap);
        ArgumentNullException.ThrowIfNull(protectionMap);
        ArgumentNullException.ThrowIfNull(effectiveDetailMap);

        ImageSize size = artisticDetailField.Size;
        if (suppressionMap.Size != size
            || protectionMap.Size != size
            || effectiveDetailMap.Size != size)
        {
            throw new ArgumentException("All background-suppression maps must have identical dimensions.");
        }

        ArtisticDetailField = artisticDetailField;
        SuppressionMap = suppressionMap;
        ProtectionMap = protectionMap;
        EffectiveDetailMap = effectiveDetailMap;
    }

    public ArtisticDetailField ArtisticDetailField { get; }

    public DetailMap SuppressionMap { get; }

    public DetailMap ProtectionMap { get; }

    public DetailMap EffectiveDetailMap { get; }

    public static BackgroundSuppressionResult CreateDisabled(DetailMap detailMap)
    {
        ArgumentNullException.ThrowIfNull(detailMap);
        DetailMap empty = DetailMap.CreateUniform(detailMap.Size, 0f);
        return new BackgroundSuppressionResult(
            ArtisticDetailField.CreateUniform(detailMap.Size, 0f),
            empty,
            empty,
            new DetailMap(detailMap.Width, detailMap.Height, detailMap.CopyValues()));
    }
}
