using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Domain.Tests.Semantics;

public sealed class SemanticRegionTests
{
    [Fact]
    public void ConstructorTrimsTextAndPreservesValues()
    {
        SemanticRegion region = new(
            " subject-1 ",
            new NormalizedRect(0.1d, 0.2d, 0.6d, 0.8d),
            0.75d,
            0.9d,
            SemanticRegionRole.Subject,
            SemanticSubjectKind.Person,
            " person ",
            " provider ");

        Assert.Equal("subject-1", region.Id);
        Assert.Equal("person", region.Label);
        Assert.Equal("provider", region.ProviderId);
        Assert.Equal(0.75d, region.Confidence);
        Assert.Equal(0.9d, region.Importance);
        Assert.Equal(SemanticRegionRole.Subject, region.Role);
        Assert.Equal(SemanticSubjectKind.Person, region.Kind);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidConfidence(double confidence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticRegion(
            "region",
            new NormalizedRect(0d, 0d, 1d, 1d),
            confidence,
            0.5d,
            SemanticRegionRole.Subject));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidImportance(double importance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticRegion(
            "region",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.5d,
            importance,
            SemanticRegionRole.Subject));
    }

    [Fact]
    public void ConstructorRejectsUnknownRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticRegion(
            "region",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.5d,
            0.5d,
            (SemanticRegionRole)999));
    }

    [Fact]
    public void ConstructorRejectsUnknownSubjectKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticRegion(
            "region",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.5d,
            0.5d,
            SemanticRegionRole.Subject,
            (SemanticSubjectKind)999));
    }
}
