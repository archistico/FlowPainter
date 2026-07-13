using System.Text;
using FlowPainter.Application.Workflow;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class RecentItemsSerializerTests
{
    [Fact]
    public async Task RoundTripPreservesProjectAndPresetPaths()
    {
        RecentItemsDocument expected = new(
            RecentItemsDocument.CurrentSchemaVersion,
            ["project-a", "project-b"],
            ["preset-a"]);
        await using MemoryStream stream = new();

        await RecentItemsSerializer.SerializeAsync(expected, stream);
        stream.Position = 0L;
        RecentItemsDocument actual = await RecentItemsSerializer.DeserializeAsync(stream);

        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.Projects, actual.Projects);
        Assert.Equal(expected.Presets, actual.Presets);
    }

    [Fact]
    public async Task SerializeTruncatesExistingStream()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(new string('x', 5000)));
        RecentItemsDocument document = new(RecentItemsDocument.CurrentSchemaVersion);

        await RecentItemsSerializer.SerializeAsync(document, stream);

        Assert.True(stream.Length < 5000L);
    }

    [Fact]
    public async Task SerializeRejectsReadOnlyStream()
    {
        await using MemoryStream stream = new([], writable: false);

        await Assert.ThrowsAsync<ArgumentException>(() => RecentItemsSerializer.SerializeAsync(
            new RecentItemsDocument(RecentItemsDocument.CurrentSchemaVersion),
            stream));
    }

    [Fact]
    public async Task DeserializeRejectsWriteOnlyStream()
    {
        await using WriteOnlyStream stream = new();

        await Assert.ThrowsAsync<ArgumentException>(() => RecentItemsSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task SerializeHonorsCancellation()
    {
        await using MemoryStream stream = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RecentItemsSerializer.SerializeAsync(
            new RecentItemsDocument(RecentItemsDocument.CurrentSchemaVersion),
            stream,
            cancellation.Token));
    }


    [Fact]
    public async Task DeserializeRejectsMissingSchemaVersion()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"projects\":[]}"));

        await Assert.ThrowsAsync<InvalidDataException>(() => RecentItemsSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task DeserializeRejectsUnsupportedSchemaVersion()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"schemaVersion\":99,\"projects\":[],\"presets\":[]}"));

        await Assert.ThrowsAsync<NotSupportedException>(() => RecentItemsSerializer.DeserializeAsync(stream));
    }

    [Fact]
    public async Task DeserializeHonorsCancellation()
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("{}"));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RecentItemsSerializer.DeserializeAsync(
            stream,
            cancellation.Token));
    }

    [Fact]
    public void DocumentCollectionsExposeReadOnlyViews()
    {
        RecentItemsDocument document = new(
            RecentItemsDocument.CurrentSchemaVersion,
            ["project"],
            ["preset"]);
        IList<string> projects = Assert.IsAssignableFrom<IList<string>>(document.Projects);
        IList<string> presets = Assert.IsAssignableFrom<IList<string>>(document.Presets);

        Assert.Throws<NotSupportedException>(() => projects.Clear());
        Assert.Throws<NotSupportedException>(() => presets.Clear());
        Assert.Single(document.Projects);
        Assert.Single(document.Presets);
    }

    [Fact]
    public void DocumentRejectsUnknownSchemaVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentItemsDocument(99));
    }

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
