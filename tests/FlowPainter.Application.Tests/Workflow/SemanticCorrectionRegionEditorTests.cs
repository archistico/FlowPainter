using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class SemanticCorrectionRegionEditorTests
{
    [Fact]
    public void AddCreatesSequentialIdentifiersAndDefaultLabels()
    {
        SemanticCorrectionRegionEditor editor = new();

        SemanticCorrectionRegion first = editor.Add(CreateBounds(), SemanticCorrectionKind.ForceSubject);
        SemanticCorrectionRegion second = editor.Add(CreateBounds(0.5d), SemanticCorrectionKind.ForceBackground);

        Assert.Equal("semantic-correction-0001", first.Id);
        Assert.Equal("Subject 1", first.Label);
        Assert.Equal("semantic-correction-0002", second.Id);
        Assert.Equal("Background 2", second.Label);
    }

    [Fact]
    public void AddingNewPrimarySubjectDemotesExistingPrimary()
    {
        SemanticCorrectionRegionEditor editor = new();
        SemanticCorrectionRegion first = editor.Add(CreateBounds(), SemanticCorrectionKind.ForcePrimarySubject);

        SemanticCorrectionRegion second = editor.Add(CreateBounds(0.5d), SemanticCorrectionKind.ForcePrimarySubject);

        Assert.Equal(SemanticCorrectionKind.ForceSubject, editor.Get(first.Id).Kind);
        Assert.Equal(SemanticCorrectionKind.ForcePrimarySubject, second.Kind);
        Assert.Single(editor.Regions, region => region.Kind == SemanticCorrectionKind.ForcePrimarySubject);
    }

    [Fact]
    public void AddReplacesExistingCorrectionForSameAutomaticRegion()
    {
        SemanticCorrectionRegionEditor editor = new();
        SemanticCorrectionRegion first = editor.Add(
            CreateBounds(),
            SemanticCorrectionKind.ForcePrimarySubject,
            "Primary",
            "semantic-subject-01");

        SemanticCorrectionRegion replacement = editor.Add(
            CreateBounds(0.5d),
            SemanticCorrectionKind.ForceBackground,
            "Background",
            "SEMANTIC-SUBJECT-01");

        Assert.Single(editor.Regions);
        Assert.Equal(first.Id, replacement.Id);
        Assert.Equal(SemanticCorrectionKind.ForceBackground, replacement.Kind);
        Assert.Equal(CreateBounds(0.5d), replacement.Bounds);
    }

    [Fact]
    public void ReplaceAllRejectsDuplicateIdentifiers()
    {
        SemanticCorrectionRegionEditor editor = new();
        SemanticCorrectionRegion first = CreateRegion("semantic-correction-0001");
        SemanticCorrectionRegion duplicate = CreateRegion("SEMANTIC-CORRECTION-0001");

        Assert.Throws<ArgumentException>(() => editor.ReplaceAll([first, duplicate]));
    }

    [Fact]
    public void ReplaceAllRejectsMultiplePrimarySubjects()
    {
        SemanticCorrectionRegionEditor editor = new();
        SemanticCorrectionRegion first = CreateRegion(
            "semantic-correction-0001",
            SemanticCorrectionKind.ForcePrimarySubject);
        SemanticCorrectionRegion second = CreateRegion(
            "semantic-correction-0002",
            SemanticCorrectionKind.ForcePrimarySubject);

        Assert.Throws<ArgumentException>(() => editor.ReplaceAll([first, second]));
    }

    [Fact]
    public void ReplaceAllAdvancesIdentifierSequence()
    {
        SemanticCorrectionRegionEditor editor = new();
        editor.ReplaceAll([CreateRegion("semantic-correction-0012")]);

        SemanticCorrectionRegion added = editor.Add(CreateBounds(0.5d), SemanticCorrectionKind.ForceSubject);

        Assert.Equal("semantic-correction-0013", added.Id);
    }

    [Fact]
    public void RemoveDeletesMatchingCorrectionIgnoringCase()
    {
        SemanticCorrectionRegionEditor editor = new();
        SemanticCorrectionRegion region = editor.Add(CreateBounds(), SemanticCorrectionKind.IgnoreAutomaticDetection);

        Assert.True(editor.Remove(region.Id.ToUpperInvariant()));
        Assert.Empty(editor.Regions);
    }

    [Fact]
    public void RegionsExposeReadOnlyView()
    {
        SemanticCorrectionRegionEditor editor = new();
        editor.Add(CreateBounds(), SemanticCorrectionKind.ForceSubject);
        IList<SemanticCorrectionRegion> regions = Assert.IsAssignableFrom<IList<SemanticCorrectionRegion>>(editor.Regions);

        Assert.Throws<NotSupportedException>(() => regions.Clear());
    }

    private static SemanticCorrectionRegion CreateRegion(
        string id,
        SemanticCorrectionKind kind = SemanticCorrectionKind.ForceSubject)
    {
        return new SemanticCorrectionRegion(id, CreateBounds(), kind);
    }

    private static NormalizedRect CreateBounds(double offset = 0.1d)
    {
        return new NormalizedRect(offset, offset, offset + 0.3d, offset + 0.3d);
    }
}
