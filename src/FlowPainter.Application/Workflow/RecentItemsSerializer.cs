using System.Text.Json;

namespace FlowPainter.Application.Workflow;

public static class RecentItemsSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task SerializeAsync(
        RecentItemsDocument document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (destination.CanSeek)
        {
            destination.Position = 0L;
            destination.SetLength(0L);
        }

        await JsonSerializer.SerializeAsync(
            destination,
            document,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<RecentItemsDocument> DeserializeAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }

        using JsonDocument jsonDocument = await JsonDocument.ParseAsync(
            source,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        JsonElement root = jsonDocument.RootElement;
        if (!root.TryGetProperty("schemaVersion", out JsonElement schemaElement)
            || !schemaElement.TryGetInt32(out int schemaVersion))
        {
            throw new InvalidDataException("The recent-items schema version is missing or invalid.");
        }

        if (schemaVersion != RecentItemsDocument.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Recent-items schema version {schemaVersion} is not supported. Supported version: {RecentItemsDocument.CurrentSchemaVersion}.");
        }

        RecentItemsDocument? document = root.Deserialize<RecentItemsDocument>(SerializerOptions);
        return document
            ?? throw new InvalidDataException("The recent-items document is empty or invalid.");
    }
}
