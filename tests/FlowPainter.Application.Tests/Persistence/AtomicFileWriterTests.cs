using System.Text;
using FlowPainter.Application.Persistence;

namespace FlowPainter.Application.Tests.Persistence;

public sealed class AtomicFileWriterTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"FlowPainter.AtomicFileWriterTests.{Guid.NewGuid():N}");

    [Fact]
    public async Task WriteAsyncCreatesNewDestination()
    {
        string destinationPath = GetDestinationPath();

        await AtomicFileWriter.WriteAsync(
            destinationPath,
            (output, cancellationToken) => WriteTextAsync(output, "new content", cancellationToken));

        Assert.Equal("new content", await File.ReadAllTextAsync(destinationPath));
        AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task WriteAsyncReplacesExistingDestination()
    {
        string destinationPath = GetDestinationPath();
        Directory.CreateDirectory(_temporaryDirectory);
        await File.WriteAllTextAsync(destinationPath, "old content");

        await AtomicFileWriter.WriteAsync(
            destinationPath,
            (output, cancellationToken) => WriteTextAsync(output, "replacement", cancellationToken));

        Assert.Equal("replacement", await File.ReadAllTextAsync(destinationPath));
        AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task WriteAsyncPreservesExistingDestinationWhenWriterFails()
    {
        string destinationPath = GetDestinationPath();
        Directory.CreateDirectory(_temporaryDirectory);
        await File.WriteAllTextAsync(destinationPath, "preserve me");

        await Assert.ThrowsAsync<InvalidOperationException>(() => AtomicFileWriter.WriteAsync(
            destinationPath,
            async (output, cancellationToken) =>
            {
                await WriteTextAsync(output, "partial replacement", cancellationToken);
                throw new InvalidOperationException("Simulated writer failure.");
            }));

        Assert.Equal("preserve me", await File.ReadAllTextAsync(destinationPath));
        AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task WriteAsyncPreservesExistingDestinationWhenCancelled()
    {
        string destinationPath = GetDestinationPath();
        Directory.CreateDirectory(_temporaryDirectory);
        await File.WriteAllTextAsync(destinationPath, "preserve me");
        using CancellationTokenSource cancellation = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileWriter.WriteAsync(
            destinationPath,
            async (output, cancellationToken) =>
            {
                await WriteTextAsync(output, "partial replacement", cancellationToken);
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            },
            cancellation.Token));

        Assert.Equal("preserve me", await File.ReadAllTextAsync(destinationPath));
        AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task WriteAsyncDoesNotCreateDestinationWhenNewWriteFails()
    {
        string destinationPath = GetDestinationPath();

        await Assert.ThrowsAsync<InvalidDataException>(() => AtomicFileWriter.WriteAsync(
            destinationPath,
            async (output, cancellationToken) =>
            {
                await WriteTextAsync(output, "partial", cancellationToken);
                throw new InvalidDataException("Simulated serialization failure.");
            }));

        Assert.False(File.Exists(destinationPath));
        AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task WriteAsyncCreatesMissingDestinationDirectory()
    {
        string destinationPath = Path.Combine(_temporaryDirectory, "nested", "project.json");

        await AtomicFileWriter.WriteAsync(
            destinationPath,
            (output, cancellationToken) => WriteTextAsync(output, "content", cancellationToken));

        Assert.Equal("content", await File.ReadAllTextAsync(destinationPath));
    }

    [Fact]
    public async Task WriteAsyncRejectsBlankDestinationPath()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => AtomicFileWriter.WriteAsync(
            " ",
            (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task WriteAsyncHonorsPreCancelledTokenWithoutTouchingDestination()
    {
        string destinationPath = GetDestinationPath();
        Directory.CreateDirectory(_temporaryDirectory);
        await File.WriteAllTextAsync(destinationPath, "preserve me");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileWriter.WriteAsync(
            destinationPath,
            (_, _) => Task.CompletedTask,
            cancellation.Token));

        Assert.Equal("preserve me", await File.ReadAllTextAsync(destinationPath));
        AssertNoTemporaryFiles();
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string GetDestinationPath()
    {
        return Path.Combine(_temporaryDirectory, "output.txt");
    }

    private void AssertNoTemporaryFiles()
    {
        if (!Directory.Exists(_temporaryDirectory))
        {
            return;
        }

        Assert.Empty(Directory.EnumerateFiles(_temporaryDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    private static async Task WriteTextAsync(
        Stream output,
        string content,
        CancellationToken cancellationToken)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);
        await output.WriteAsync(data, cancellationToken);
    }
}
