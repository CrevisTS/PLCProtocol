namespace PLCCommunication_v2.Mitsubishi
{
    public class PLCSendingPacket : IPLCSendingPacket
    {
        public EPLCDeviceCode DeviceCode { get; }

        public int Address { get; }

        public object Value { get; }

        public bool IsRead { get; }

        public ushort WordCount { get; }

        public PLCSendingPacket(EPLCDeviceCode code, int address, bool isRead, ushort readWordCount)
        {
            this.DeviceCode = code;
            this.Address = address;
            this.Value = (object)(byte)0;
            this.WordCount = readWordCount;
            this.IsRead = true;
        }

        public PLCSendingPacket(EPLCDeviceCode code, int address, object value)
        {
            this.DeviceCode = code;
            this.Address = address;
            this.Value = value;
            this.WordCount = (ushort)0;
            this.IsRead = false;
        }
    }
}
