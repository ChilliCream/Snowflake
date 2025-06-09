/// <summary>
/// A lock-free, thread-safe, 64-bit ID generator that follows the Snowflake ID 
/// format which is based on twitters algorithm.
/// https://blog.x.com/engineering/en_us/a/2010/announcing-snowflake
/// </summary>
public sealed class IdGenerator
{
    private const int TimestampBits = 41;
    private const int DatacenterBits = 5;
    private const int MachineBits = 5;
    private const int SequenceBits = 12;

    private const long MaxTimestamp = (1L << TimestampBits) - 1;
    private const long MaxDatacenter = (1L << DatacenterBits) - 1;
    private const long MaxMachine = (1L << MachineBits) - 1;
    private const long MaxSequence = (1L << SequenceBits) - 1;

    private const int DatacenterShift = SequenceBits + MachineBits;
    private const int MachineShift = SequenceBits;
    private const int TimestampShift = SequenceBits + MachineBits + DatacenterBits;

    private readonly long _datacenterId;
    private readonly long _machineId;
    private readonly long _epoch;
    private long _lastId = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class.
    /// </summary>
    /// <param name="datacenterId">
    /// The datacenter ID within the range of 0 to 31.
    /// </param>
    /// <param name="machineId">
    /// The machine ID within the range of 0 to 31.
    /// </param>
    /// <param name="epochUtc">
    /// The epoch UTC.
    /// </param>
    public IdGenerator(int datacenterId, int machineId, DateTime epochUtc)
    {
        if (datacenterId < 0 || datacenterId > MaxDatacenter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datacenterId),
                $"Datacenter ID must be between 0 and {MaxDatacenter}.");
        }
        
        if (machineId < 0 || machineId > MaxMachine)
        {
            throw new ArgumentOutOfRangeException(
                nameof(machineId),
                $"Machine ID must be between 0 and {MaxMachine}.");
        }

        _datacenterId = datacenterId;
        _machineId = machineId;
        _epoch = new DateTimeOffset(epochUtc).ToUnixTimeMilliseconds();
    }

    public long NextId()
    {
        while (true)
        {
            var timestamp = GetCurrentTimestamp();

            if (timestamp < 0)
            {
                throw new InvalidOperationException("Clock moved backwards");
            }

            // We have space for 69 years in the 41 bits we reserved for the timestamp.
            // If we run out of space, we throw an exception.
            if (timestamp > MaxTimestamp)
            {
                throw new InvalidOperationException("Timestamp overflow");
            }

            // We first get the last id.
            var lastId = Volatile.Read(ref _lastId);
            var lastTime = lastId >> SequenceBits;
            var sequence = lastId & MaxSequence;

            // If the timestamp is the same as was used for the last id, 
            // we increment the sequence.
            if (timestamp == lastTime)
            {
                sequence = (sequence + 1) & MaxSequence;

                // The sequence has space for 4096 ids per millisecond.
                // If we run out of space, we wait until the next millisecond and try again.
                if (sequence == 0)
                {
                    timestamp = WaitUntilNextMillisecond(lastTime);
                }
            }
            else
            {
                // If the timestamp is different, we reset the sequence.
                sequence = 0;
            }

            // With the updated sequence we can now build a new id state which we will attempt 
            // to set.
            var newId = (timestamp << SequenceBits) | sequence;

            // Since we are using compare exchange to set the state, the state will only be set if
            // the _lastId has the same value as the value we have read, which means that there
            // was no other thread that has incremented the sequence in between the time we read
            // the last id and the time we set the new id.
            if (Interlocked.CompareExchange(ref _lastId, newId, lastId) == lastId)
            {
                // if we were successful, we return the new id.
                return (timestamp << TimestampShift) |
                       (_datacenterId << DatacenterShift) |
                       (_machineId << MachineShift) |
                        sequence;
            }
        }
    }

    private long GetCurrentTimestamp()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _epoch;

    private long WaitUntilNextMillisecond(long lastTimestamp)
    {
        var spin = new SpinWait();
        long timestamp;
        do
        {
            spin.SpinOnce();
            timestamp = GetCurrentTimestamp();
        } while (timestamp <= lastTimestamp);
        return timestamp;
    }
}