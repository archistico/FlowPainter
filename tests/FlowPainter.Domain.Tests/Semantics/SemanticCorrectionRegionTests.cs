using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Domain.Tests.Semantics;

public sealed class SemanticCorrectionRegionTests
{
    [Fact]
    public void ConstructorTrimsOptionalText()
    {
        SemanticCorrectionRegion region = new(
            " correction-1 ",
            CreateBounds(),
            SemanticCorrectionKind.ForcePrimarySubject,
            " Main subject ",
            " semantic-subject-01 ");

        Assert.Equal("correction-1", region.Id);
        Assert.Equal("Main subject", region.Label);
        Assert.Equal("semantic-subject-01", region.SourceSemanticRegionId);
    }

    [Fact]
    public void ConstructorNormalizesBlankOptionalTextToNull()
    {
        SemanticCorrectionRegion region = new(
            "correction-1",
            CreateBounds(),
            SemanticCorrectionKind.ForceSubject,
            " ",
            null);

        Assert.Null(region.Label);
        Assert.Null(region.SourceSemanticRegionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsBlankIdentifier(string id)
    {
        Assert.Throws<ArgumentException>(() => new SemanticCorrectionRegion(
            id,
            CreateBounds(),
            SemanticCorrectionKind.ForceBackground));
    }

    [Fact]
    public void ConstructorRejectsUnknownKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticCorrectionRegion(
            "correction-1",
            CreateBounds(),
            (SemanticCorrectionKind)999));
    }

    private static NormalizedRect CreateBounds()
    {
        return new NormalizedRect(0.1d, 0.2d, 0.6d, 0.8d);
    }
}
