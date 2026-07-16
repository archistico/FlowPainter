using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Imaging.Skia.Tests.Images;

public sealed class SkiaImageLoaderTests
{
    [Fact]
    public async Task LoadAsyncDecodesDimensionsPixelsAndSourceName()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(
            2,
            2,
            (x, y) => (x, y) switch
            {
                (0, 0) => SKColors.Red,
                (1, 0) => SKColors.Green,
                (0, 1) => SKColors.Blue,
                _ => SKColors.White
            });
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();

        using SkiaImage image = await loader.LoadAsync(stream, "  fixture.png  ");

        Assert.Equal(new ImageSize(2, 2), image.Size);
        Assert.Equal("fixture.png", image.SourceName);
        Assert.Equal(Rgba32.Opaque(255, 0, 0), image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(Rgba32.Opaque(255, 255, 255), image.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public async Task LoadAsyncReportsOrderedStages()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        RecordingProgress<ImageOperationProgress> progress = new();
        SkiaImageLoader loader = new();

        using SkiaImage image = await loader.LoadAsync(stream, progress: progress);

        ImageOperationStage[] expectedStages =
        [
            ImageOperationStage.ReadingEncodedData,
            ImageOperationStage.InspectingMetadata,
            ImageOperationStage.DecodingPixels,
            ImageOperationStage.Completed
        ];

        Assert.Equal(expectedStages, progress.Values.Select(value => value.Stage));
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public async Task LoadAsyncRejectsUnsupportedImageDimensions()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(
            ImageSize.MaximumDimension + 1,
            1,
            (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();

        UnsupportedImageDimensionsException exception = await Assert.ThrowsAsync<UnsupportedImageDimensionsException>(
            () => loader.LoadAsync(stream));

        Assert.Equal(ImageSize.MaximumDimension + 1, exception.Width);
        Assert.Equal(1, exception.Height);
    }

    [Fact]
    public async Task LoadAsyncRejectsInvalidEncodedData()
    {
        using MemoryStream stream = new(new byte[] { 1, 2, 3, 4 }, writable: false);
        SkiaImageLoader loader = new();

        await Assert.ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsyncRejectsSeekableInputAboveConfiguredEncodedLimit()
    {
        using MemoryStream stream = new(new byte[17], writable: false);
        SkiaImageLoader loader = new(maximumEncodedImageBytes: 16);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => loader.LoadAsync(stream));

        Assert.Contains("input limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsyncRejectsNonSeekableInputAsSoonAsLimitIsExceeded()
    {
        using NonSeekableReadStream stream = new(new byte[17]);
        SkiaImageLoader loader = new(maximumEncodedImageBytes: 16);

        await Assert.ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(stream));
    }

    [Fact]
    public async Task LoadAsyncHonorsCancellationDuringNonSeekableStreaming()
    {
        using CancellationTokenSource cancellation = new();
        using CancellingNonSeekableReadStream stream = new(new byte[64], cancellation);
        SkiaImageLoader loader = new(maximumEncodedImageBytes: 128);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => loader.LoadAsync(stream, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task LoadAsyncHonorsPreCancelledToken()
    {
        using MemoryStream stream = new(new byte[] { 1, 2, 3, 4 }, writable: false);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SkiaImageLoader loader = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => loader.LoadAsync(stream, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task DisposedImageRejectsSamplingAndEncoding()
    {
        byte[] png = SkiaTestImageFactory.CreatePng(1, 1, (_, _) => SKColors.Black);
        using MemoryStream stream = new(png, writable: false);
        SkiaImageLoader loader = new();
        SkiaImage image = await loader.LoadAsync(stream);

        image.Dispose();

        Assert.True(image.IsDisposed);
        Assert.Throws<ObjectDisposedException>(
            () => image.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Throws<ObjectDisposedException>(() => image.EncodePng());
    }

    private sealed class CancellingNonSeekableReadStream : Stream
    {
        private readonly byte[] _data;
        private readonly CancellationTokenSource _cancellation;
        private bool _readStarted;

        public CancellingNonSeekableReadStream(
            byte[] data,
            CancellationTokenSource cancellation)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(cancellation);
            _data = data;
            _cancellation = cancellation;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_readStarted)
            {
                return ValueTask.FromResult(0);
            }

            _readStarted = true;
            int copied = Math.Min(_data.Length, Math.Min(buffer.Length, 8));
            _data.AsMemory(0, copied).CopyTo(buffer);
            _cancellation.Cancel();
            return ValueTask.FromResult(copied);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly byte[] _data;
        private int _position;

        public NonSeekableReadStream(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            _data = data;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = _data.Length - _position;
            int copied = Math.Min(remaining, count);
            _data.AsSpan(_position, copied).CopyTo(buffer.AsSpan(offset, copied));
            _position += copied;
            return copied;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = _data.Length - _position;
            int copied = Math.Min(remaining, buffer.Length);
            _data.AsMemory(_position, copied).CopyTo(buffer);
            _position += copied;
            return ValueTask.FromResult(copied);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

}
