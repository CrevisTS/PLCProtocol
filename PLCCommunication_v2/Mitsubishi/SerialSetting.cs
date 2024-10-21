using System.IO.Ports;

namespace PLCCommunication_v2.Mitsubishi
{
    public class SerialSetting
    {
        public string PortName { get; set; }

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        public Parity Parity { get; set; }

        public StopBits StopBits { get; set; }

        public Handshake Handshake { get; set; }

        public byte PCNo { get; set; }

        public byte NetworkNo { get; set; }

        public byte HostStationNo { get; set; }

        public ushort ReconnectCount { get; set; }
    }
}

