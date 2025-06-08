using System.Collections.Concurrent;

namespace ChilliCream.Snowflake.Tests;

public class IdGeneratorTests
{
    private static readonly DateTime s_epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(32, 0)]
    public void Ctor_InvalidDatacenter_Throws(int dcId, int machineId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IdGenerator(dcId, machineId, s_epoch));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 32)]
    public void Ctor_InvalidMachine_Throws(int dcId, int machineId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IdGenerator(dcId, machineId, s_epoch));
    }

    [Fact]
    public void NextId_SequentialCalls_AreUniqueAndOrdered()
    {
        // Arrange
        var generator = new IdGenerator(3, 7, s_epoch);
        var ids = new List<long>();

        // Act
        for (int i = 0; i < 10_000; i++)
        {
            ids.Add(generator.NextId());
        }

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
        for (int i = 1; i < ids.Count; i++)
        {
            Assert.True(
                ids[i] > ids[i - 1],
                $"ID at index {i} is not greater than previous one.");
        }
    }

    [Fact]
    public async Task NextId_MultiThreaded_GeneratesUniqueIds()
    {
        // Arrange
        var generator = new IdGenerator(1, 2, s_epoch);
        var bag = new ConcurrentBag<long>();
        var threads = 8;
        var idsPerThread = 5_000;
        var tasks = new Task[threads];

        // Act
        for (var t = 0; t < threads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < idsPerThread; i++)
                {
                    bag.Add(generator.NextId());
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(threads * idsPerThread, bag.Count);
        Assert.Equal(bag.Distinct().Count(), bag.Count);
    }

    [Fact]
    public void NextId_ContainsExpectedDatacenterAndMachineBits()
    {
        // Arrange
        var datacenterId = 12;
        var machineId = 27;
        var generator = new IdGenerator(datacenterId, machineId, s_epoch);

        // Act
        var id = generator.NextId();

        // Assert
        Assert.Equal(datacenterId, IdInspector.GetDatacenter(id));
        Assert.Equal(machineId, IdInspector.GetMachine(id));
    }

    [Fact]
    public void Sequence_Resets_WhenMillisecondChanges()
    {
        // Arrange
        var generator = new IdGenerator(0, 0, s_epoch);

        // Act
        var firstId = generator.NextId();
        var firstTimestamp = IdInspector.GetTimestamp(firstId);
        var firstSeq = IdInspector.GetSequence(firstId);
        long secondId = -1;

        // Busy‑wait until we move to next millisecond
        SpinWait.SpinUntil(() =>
        {
            var nextId = generator.NextId();
            if (IdInspector.GetTimestamp(nextId) > firstTimestamp)
            {
                secondId = nextId;
                return true;
            }

            return false;
        });

        var secondTimestamp = IdInspector.GetTimestamp(secondId);
        var secondSeq = IdInspector.GetSequence(secondId);

        // Assert
        Assert.True(
            secondTimestamp > firstTimestamp,
            "Timestamp did not move forward to next millisecond.");
        Assert.Equal(0, secondSeq);
    }
}
