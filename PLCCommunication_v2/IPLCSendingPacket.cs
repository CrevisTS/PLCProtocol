namespace PLCCommunication_v2
{
    public interface IPLCSendingPacket
    {
        int Address { get; }

        object Value { get; }
    }
}
