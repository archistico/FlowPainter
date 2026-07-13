using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Projects;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Tests.Projects;

public sealed class FlowPainterProjectTests
{
    [Fact]
    public void ConstructorTrimsNameAndSourcePath()
    {
        FlowPainterProject project = new("  Portrait  ", "  images/source.png  ", 42UL, new FlowPainterSettings());

        Assert.Equal("Portrait", project.Name);
        Assert.Equal("images/source.png", project.SourcePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new FlowPainterProject(name, "source.png", 1UL, new FlowPainterSettings()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsEmptySourcePath(string sourcePath)
    {
        Assert.Throws<ArgumentException>(() => new FlowPainterProject("Project", sourcePath, 1UL, new FlowPainterSettings()));
    }

    [Fact]
    public void ConstructorCopiesRegionCollection()
    {
        List<DetailRegion> regions = [CreateRegion("manual-0001")];
        FlowPainterProject project = new("Project", "source.png", 1UL, new FlowPainterSettings(), detailRegions: regions);

        regions.Clear();

        Assert.Single(project.DetailRegions);
    }

    [Fact]
    public void DetailRegionsExposeReadOnlyView()
    {
        FlowPainterProject project = new(
            "Project",
            "source.png",
            1UL,
            new FlowPainterSettings(),
            detailRegions: [CreateRegion("manual-0001")]);
        IList<DetailRegion> regions = Assert.IsAssignableFrom<IList<DetailRegion>>(project.DetailRegions);

        Assert.Throws<NotSupportedException>(() => regions.Add(CreateRegion("manual-0002")));
        Assert.Single(project.DetailRegions);
    }

    [Fact]
    public void ConstructorRejectsDuplicateRegionIdentifiersIgnoringCase()
    {
        DetailRegion[] regions =
        [
            CreateRegion("manual-0001"),
            CreateRegion("MANUAL-0001")
        ];

        Assert.Throws<ArgumentException>(() => new FlowPainterProject(
            "Project",
            "source.png",
            1UL,
            new FlowPainterSettings(),
            detailRegions: regions));
    }

    [Fact]
    public void ConstructorUsesStandardPreviewByDefault()
    {
        FlowPainterProject project = new("Project", "source.png", 1UL, new FlowPainterSettings());

        Assert.Equal(PreviewQuality.Standard, project.Preview.Quality);
    }

    [Fact]
    public void ConstructorUsesDefaultFinalRenderSettings()
    {
        FlowPainterProject project = new("Project", "source.png", 1UL, new FlowPainterSettings());

        Assert.Equal(FinalRenderSettings.DefaultMaximumDimension, project.FinalRender.MaximumDimension);
        Assert.Equal(FlowPainter.Domain.Images.RasterImageFormat.Png, project.FinalRender.Format);
    }

    [Fact]
    public void ConstructorPreservesExplicitFinalRenderSettings()
    {
        FinalRenderSettings finalRender = new(8000, FlowPainter.Domain.Images.RasterImageFormat.Jpeg, 88);

        FlowPainterProject project = new(
            "Project",
            "source.png",
            1UL,
            new FlowPainterSettings(),
            finalRender: finalRender);

        Assert.Same(finalRender, project.FinalRender);
    }

    private static DetailRegion CreateRegion(string id)
    {
        return new DetailRegion(
            id,
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.6d),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Face");
    }
}
