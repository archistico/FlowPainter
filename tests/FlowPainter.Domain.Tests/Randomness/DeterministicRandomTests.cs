using FlowPainter.Domain.Randomness;

namespace FlowPainter.Domain.Tests.Randomness;

public sealed class DeterministicRandomTests
{
    [Fact]
    public void NextUInt64ProducesStableGoldenSequence()
    {
        DeterministicRandom random = new(1UL);

        ulong[] values =
        [
            random.NextUInt64(),
            random.NextUInt64(),
            random.NextUInt64(),
            random.NextUInt64(),
            random.NextUInt64()
        ];

        ulong[] expected =
        [
            0x910A2DEC89025CC1UL,
            0xBEEB8DA1658EEC67UL,
            0xF893A2EEFB32555EUL,
            0x71C18690EE42C90BUL,
            0x71BB54D8D101B5B9UL
        ];

        Assert.Equal(expected, values);
    }

    [Fact]
    public void EqualSeedsProduceEqualSequences()
    {
        DeterministicRandom first = new(42UL);
        DeterministicRandom second = new(42UL);

        for (int index = 0; index < 100; index++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void NextDoubleAlwaysReturnsUnitIntervalValue()
    {
        DeterministicRandom random = new(7UL);

        for (int index = 0; index < 1_000; index++)
        {
            double value = random.NextDouble();
            Assert.InRange(value, 0d, 0.9999999999999999d);
        }
    }
}
