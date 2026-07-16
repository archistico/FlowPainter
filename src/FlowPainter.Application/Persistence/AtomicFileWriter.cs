namespace FlowPainter.Application.Persistence;

public static class AtomicFileWriter
{
    private const int BufferSize = 81920;

    public static async Task WriteAsync(
        string destinationPath,
        Func<Stream, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("The destination path is required.", nameof(destinationPath));
        }

        ArgumentNullException.ThrowIfNull(writeAsync);
        cancellationToken.ThrowIfCancellationRequested();

        string fullDestinationPath = Path.GetFullPath(destinationPath);
        string destinationDirectory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new ArgumentException("The destination path must include a valid directory.", nameof(destinationPath));
        Directory.CreateDirectory(destinationDirectory);

        string temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");
        bool committed = false;

        try
        {
            await using (FileStream output = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await writeAsync(output, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            CommitTemporaryFile(temporaryPath, fullDestinationPath);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }
    }

    private static void CommitTemporaryFile(
        string temporaryPath,
        string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(
                temporaryPath,
                destinationPath,
                destinationBackupFileName: null,
                ignoreMetadataErrors: true);
            return;
        }

        File.Move(temporaryPath, destinationPath);
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (IOException)
        {
            // Cleanup is best-effort and must not replace the original write failure.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best-effort and must not replace the original write failure.
        }
    }
}
