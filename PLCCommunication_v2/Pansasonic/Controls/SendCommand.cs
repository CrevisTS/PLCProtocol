namespace PLCCommunication_v2.Panasonic.Controls
{
    public class SendCommand
    {
        private int m_DeviceNumber;
        private string m_DeviceHexNumber;

        public EPLCDeviceCode DeviceCode { get; set; }

        public int Address { get; set; }

        public string ContactAddress { get; set; }

        public bool IsContact { get; set; }

        public ushort WordCount { get; set; }

        public EParseDataType DataType { get; set; }

        public string Value { get; set; }

        public SendCommand()
        {
            this.DeviceCode = EPLCDeviceCode.D;
            this.DataType = EParseDataType.Short;
            this.Value = string.Empty;
        }
    }
}
