namespace FlowPainter.Application.Tests.PrimitiveGeneration;

internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value)
    {
        Values.Add(value);
    }
}
