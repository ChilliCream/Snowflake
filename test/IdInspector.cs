namespace ChilliCream.Snowflake.Tests;

internal static class IdInspector
{
    private const int SequenceBits = 12;
    private const int MachineBits = 5;
    private const int DatacenterBits = 5;

    private const int MachineShift = SequenceBits;
    private const int DatacenterShift = SequenceBits + MachineBits;
    private const int TimestampShift = SequenceBits + MachineBits + DatacenterBits;

    private const long SequenceMask = (1L << SequenceBits) - 1;
    private const long MachineMask = ((1L << MachineBits) - 1) << MachineShift;
    private const long DatacenterMask = ((1L << DatacenterBits) - 1) << DatacenterShift;

    public static long GetTimestamp(long id) => id >> TimestampShift;

    public static int GetDatacenter(long id) => (int)((id & DatacenterMask) >> DatacenterShift);

    public static int GetMachine(long id) => (int)((id & MachineMask) >> MachineShift);

    public static int GetSequence(long id) => (int)(id & SequenceMask);
}
