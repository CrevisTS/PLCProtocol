using System.Net;

namespace PLCCommunication_v2.Mitsubishi
{
    public class SocketSetting
    {
        private IPAddress m_IPAddress;
        private string m_IP;

        public string IP
        {
            get => this.m_IP;
            set
            {
                string[] strArray = value.Split('.');
                if (strArray.Length != 4)
                    return;
                byte[] address = new byte[4];
                for (int index = 0; index < 4; ++index)
                {
                    byte result;
                    if (!byte.TryParse(strArray[index], out result))
                        return;
                    address[index] = result;
                }
                this.m_IP = value;
                this.m_IPAddress = new IPAddress(address);
            }
        }

        public int PortNumber { get; set; }

        public byte PCNo { get; set; }

        public byte NetworkNo { get; set; }

        public EPLCProtocolFormat ProtocolFormat { get; set; }

        public uint Timeout { get; set; }

        public ushort ReconnectCount { get; set; }

        public IPAddress GetIPAddress() => this.m_IPAddress;
    }
}
