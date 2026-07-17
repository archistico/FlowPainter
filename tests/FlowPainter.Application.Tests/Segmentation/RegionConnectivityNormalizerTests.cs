using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionConnectivityNormalizerTests
{
    private static readonly int[] DisconnectedFixture =
    [
        0, 1, 0,
        1, 1, 1,
        0, 1, 0,
    ];

    private static readonly int[] UndersizedFixture =
    [
        0, 0, 1,
        0, 2, 1,
    ];

    private static readonly int[] TieFixture =
    [
        0, 0, 2, 1, 1,
        0, 0, 2, 1, 1,
        0, 0, 2, 1, 1,
    ];

    private static readonly int[] CompactFixture = [3, 3, 1, 1];
    private static readonly uint[] ExpectedCompactLabels = [0, 0, 1, 1];
    private static readonly int[] SingleRegionFixture = [0, 0, 0, 0];
    private static readonly int[] InvalidRawLabelFixture = [0, 1];
    private static readonly int[] WrongCountFixture = [0, 0];

    [Fact]
    public void NormalizeSplitsDisconnectedRawLabels()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(3, 3),
            2,
            DisconnectedFixture,
            1);

        Assert.Equal(5, result.RawComponentCount);
        Assert.Equal(3, result.DisconnectedComponentsRepaired);
        Assert.Equal(5, result.Labels.RegionCount);
        AssertAllRegionsConnected(result.Labels);
    }

    [Fact]
    public void NormalizeMergesUndersizedComponentIntoBestNeighbor()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(3, 2),
            3,
            UndersizedFixture,
            2);

        Assert.Equal(1, result.UndersizedComponentsMerged);
        Assert.Equal(2, result.Labels.RegionCount);
        Assert.Equal(result.Labels[0, 0], result.Labels[1, 1]);
        AssertAllRegionsConnected(result.Labels);
    }

    [Fact]
    public void NormalizeUsesStableTieBreaking()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(5, 3),
            3,
            TieFixture,
            4);

        Assert.Equal(result.Labels[0, 0], result.Labels[2, 0]);
        Assert.NotEqual(result.Labels[4, 0], result.Labels[2, 0]);
    }

    [Fact]
    public void NormalizeCompactsLabelsByFirstAppearance()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(4, 1),
            4,
            CompactFixture,
            1);

        Assert.Equal(ExpectedCompactLabels, result.Labels.CopyLabels());
    }

    [Fact]
    public void NormalizeKeepsSingleRegionWhenMinimumExceedsImageArea()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(2, 2),
            1,
            SingleRegionFixture,
            100);

        Assert.Equal(1, result.Labels.RegionCount);
        Assert.Equal(0, result.UndersizedComponentsMerged);
    }

    [Fact]
    public void NormalizeIsDeterministic()
    {
        RegionConnectivityResult first = RegionConnectivityNormalizer.Normalize(
            new ImageSize(3, 3),
            2,
            DisconnectedFixture,
            2);
        RegionConnectivityResult second = RegionConnectivityNormalizer.Normalize(
            new ImageSize(3, 3),
            2,
            DisconnectedFixture,
            2);

        Assert.Equal(first.Labels.CopyLabels(), second.Labels.CopyLabels());
        Assert.Equal(first.UndersizedComponentsMerged, second.UndersizedComponentsMerged);
    }

    [Fact]
    public void NormalizeRejectsOutOfRangeRawLabel()
    {
        Assert.Throws<ArgumentException>(() => RegionConnectivityNormalizer.Normalize(
            new ImageSize(2, 1),
            1,
            InvalidRawLabelFixture,
            1));
    }

    [Fact]
    public void NormalizeRejectsWrongLabelCount()
    {
        Assert.Throws<ArgumentException>(() => RegionConnectivityNormalizer.Normalize(
            new ImageSize(2, 2),
            1,
            WrongCountFixture,
            1));
    }

    [Fact]
    public void NormalizeHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => RegionConnectivityNormalizer.Normalize(
            new ImageSize(2, 2),
            1,
            SingleRegionFixture,
            1,
            cancellation.Token));
    }

    [Fact]
    public void NormalizePreservesCompleteCoverage()
    {
        RegionConnectivityResult result = RegionConnectivityNormalizer.Normalize(
            new ImageSize(3, 2),
            3,
            UndersizedFixture,
            2);

        Assert.Equal(6L, result.Labels.CountPixelsByRegion().Sum(count => (long)count));
        Assert.DoesNotContain(result.Labels.CountPixelsByRegion(), count => count <= 0);
    }

    private static void AssertAllRegionsConnected(RegionLabelMap labels)
    {
        bool[] visited = new bool[checked((int)labels.Size.PixelCount)];
        int[] components = new int[labels.RegionCount];
        Queue<int> queue = new();

        for (int index = 0; index < visited.Length; index++)
        {
            if (visited[index])
            {
                continue;
            }

            int x = index % labels.Size.Width;
            int y = index / labels.Size.Width;
            uint regionId = labels[x, y];
            components[checked((int)regionId)]++;
            visited[index] = true;
            queue.Enqueue(index);
            while (queue.TryDequeue(out int current))
            {
                int currentX = current % labels.Size.Width;
                int currentY = current / labels.Size.Width;
                Enqueue(currentX - 1, currentY);
                Enqueue(currentX + 1, currentY);
                Enqueue(currentX, currentY - 1);
                Enqueue(currentX, currentY + 1);
            }

            void Enqueue(int neighborX, int neighborY)
            {
                if (neighborX < 0
                    || neighborX >= labels.Size.Width
                    || neighborY < 0
                    || neighborY >= labels.Size.Height
                    || labels[neighborX, neighborY] != regionId)
                {
                    return;
                }

                int neighborIndex = checked((neighborY * labels.Size.Width) + neighborX);
                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    queue.Enqueue(neighborIndex);
                }
            }
        }

        Assert.All(components, count => Assert.Equal(1, count));
    }
}
