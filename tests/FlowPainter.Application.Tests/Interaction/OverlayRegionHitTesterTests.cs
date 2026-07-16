using FlowPainter.Application.Interaction;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Interaction;

public sealed class OverlayRegionHitTesterTests
{
    private static readonly NormalizedPoint HitPoint = new(0.5d, 0.5d);

    [Fact]
    public void SelectDetailRegionPrefersMostRecentlyAddedRegion()
    {
        DetailRegion first = CreateDetailRegion("manual-0001");
        DetailRegion second = CreateDetailRegion("manual-0002");

        DetailRegion? selected = OverlayRegionHitTester.SelectDetailRegion([first, second], HitPoint);

        Assert.Same(second, selected);
    }

    [Fact]
    public void SelectDetailRegionCyclesOverOverlappingRegions()
    {
        DetailRegion first = CreateDetailRegion("manual-0001");
        DetailRegion second = CreateDetailRegion("manual-0002");

        DetailRegion? selected = OverlayRegionHitTester.SelectDetailRegion(
            [first, second],
            HitPoint,
            second.Id);

        Assert.Same(first, selected);
    }

    [Fact]
    public void SelectSemanticCorrectionPrefersMostRecentCorrection()
    {
        SemanticCorrectionRegion first = CreateCorrection("semantic-correction-0001");
        SemanticCorrectionRegion second = CreateCorrection("semantic-correction-0002");

        SemanticCorrectionRegion? selected = OverlayRegionHitTester.SelectSemanticCorrection(
            [first, second],
            HitPoint);

        Assert.Same(second, selected);
    }

    [Fact]
    public void SelectAutomaticSemanticRegionPrefersSmallestContainingRegion()
    {
        SemanticRegion subject = new(
            "subject",
            new NormalizedRect(0.1d, 0.1d, 0.9d, 0.9d),
            0.8d,
            0.8d,
            SemanticRegionRole.Subject);
        SemanticRegion focus = new(
            "focus",
            new NormalizedRect(0.4d, 0.4d, 0.6d, 0.6d),
            0.9d,
            0.9d,
            SemanticRegionRole.FocalArea);

        SemanticRegion? selected = OverlayRegionHitTester.SelectAutomaticSemanticRegion(
            [subject, focus],
            HitPoint);

        Assert.Same(focus, selected);
    }

    [Fact]
    public void SelectionReturnsNullOutsideAllRegions()
    {
        DetailRegion region = CreateDetailRegion("manual-0001");

        DetailRegion? selected = OverlayRegionHitTester.SelectDetailRegion(
            [region],
            new NormalizedPoint(0.95d, 0.95d));

        Assert.Null(selected);
    }

    private static DetailRegion CreateDetailRegion(string id)
    {
        return new DetailRegion(
            id,
            new NormalizedRect(0.2d, 0.2d, 0.8d, 0.8d),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
    }

    private static SemanticCorrectionRegion CreateCorrection(string id)
    {
        return new SemanticCorrectionRegion(
            id,
            new NormalizedRect(0.2d, 0.2d, 0.8d, 0.8d),
            SemanticCorrectionKind.ForceSubject);
    }
}
