using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Segmentation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class LegacySemanticCorrectionAdapterTests
{
    [Theory]
    [InlineData(SemanticCorrectionKind.ForcePrimarySubject, RegionRole.Focal)]
    [InlineData(SemanticCorrectionKind.ForceSubject, RegionRole.Subject)]
    [InlineData(SemanticCorrectionKind.ForceBackground, RegionRole.Background)]
    [InlineData(SemanticCorrectionKind.IgnoreAutomaticDetection, RegionRole.Ignore)]
    public void ConvertMapsEveryLegacyCorrectionKind(
        SemanticCorrectionKind correctionKind,
        RegionRole expectedRole)
    {
        SemanticCorrectionRegion correction = new(
            "legacy-1",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.6d),
            correctionKind,
            "Role",
            "detected-1");

        RegionRoleOverride converted = Assert.Single(
            LegacySemanticCorrectionAdapter.Convert([correction]));

        Assert.Equal(expectedRole, converted.Role);
        Assert.Equal(correction.Id, converted.Id);
        Assert.Equal(correction.Bounds, converted.Bounds);
        Assert.Equal(correction.Label, converted.Label);
        Assert.Equal(correction.SourceSemanticRegionId, converted.SourceRegionId);
    }

    [Fact]
    public void ConvertReturnsDetachedReadOnlySnapshot()
    {
        List<SemanticCorrectionRegion> corrections =
        [
            new SemanticCorrectionRegion(
                "legacy-1",
                new NormalizedRect(0d, 0d, 0.5d, 0.5d),
                SemanticCorrectionKind.ForceSubject)
        ];

        IReadOnlyList<RegionRoleOverride> converted = LegacySemanticCorrectionAdapter.Convert(corrections);
        corrections.Clear();

        Assert.Single(converted);
        Assert.IsAssignableFrom<IReadOnlyList<RegionRoleOverride>>(converted);
    }

    [Fact]
    public void ConvertRejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() => LegacySemanticCorrectionAdapter.Convert(null!));
    }
}
