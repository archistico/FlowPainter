using FlowPainter.Application.Projects;

namespace FlowPainter.Application.Tests.Projects;

public sealed class ProjectPathResolverTests
{
    [Fact]
    public void ResolveSourcePathReturnsAbsoluteReferenceUnchanged()
    {
        string sourcePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "source.png"));
        string projectPath = Path.Combine(Path.GetTempPath(), "project.flowpainter.json");

        string resolved = ProjectPathResolver.ResolveSourcePath(projectPath, sourcePath);

        Assert.Equal(sourcePath, resolved);
    }

    [Fact]
    public void ResolveSourcePathCombinesRelativeReferenceWithProjectDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "flowpainter-project-test");
        string projectPath = Path.Combine(root, "projects", "portrait.flowpainter.json");

        string resolved = ProjectPathResolver.ResolveSourcePath(projectPath, Path.Combine("..", "images", "source.png"));

        Assert.Equal(Path.GetFullPath(Path.Combine(root, "images", "source.png")), resolved);
    }

    [Fact]
    public void CreateSourceReferenceProducesPortableRelativePath()
    {
        string root = Path.Combine(Path.GetTempPath(), "flowpainter-project-test");
        string projectPath = Path.Combine(root, "projects", "portrait.flowpainter.json");
        string sourcePath = Path.Combine(root, "images", "source.png");

        string reference = ProjectPathResolver.CreateSourceReference(projectPath, sourcePath);

        Assert.Equal(Path.Combine("..", "images", "source.png"), reference);
    }

    [Fact]
    public void CreatedReferenceRoundTripsThroughResolver()
    {
        string root = Path.Combine(Path.GetTempPath(), "flowpainter-project-test");
        string projectPath = Path.Combine(root, "portrait.flowpainter.json");
        string sourcePath = Path.Combine(root, "images", "source.png");

        string reference = ProjectPathResolver.CreateSourceReference(projectPath, sourcePath);
        string resolved = ProjectPathResolver.ResolveSourcePath(projectPath, reference);

        Assert.Equal(Path.GetFullPath(sourcePath), resolved);
    }

    [Theory]
    [InlineData("", "source.png")]
    [InlineData("project.json", "")]
    public void MethodsRejectEmptyPaths(string projectPath, string sourcePath)
    {
        Assert.Throws<ArgumentException>(() => ProjectPathResolver.ResolveSourcePath(projectPath, sourcePath));
        Assert.Throws<ArgumentException>(() => ProjectPathResolver.CreateSourceReference(projectPath, sourcePath));
    }
}
