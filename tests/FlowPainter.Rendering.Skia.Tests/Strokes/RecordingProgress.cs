namespace FlowPainter.Rendering.Skia.Tests.Strokes;

internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value)
    {
        Values.Add(value);
    }
}
