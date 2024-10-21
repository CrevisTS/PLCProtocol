using System;
using System.Globalization;

namespace PLCCommunication_v2.Mitsubishi.Controls
{
    public class SendCommand
    {
        private int m_DeviceNumber;
        private string m_DeviceHexNumber;

        public EPLCDeviceCode DeviceCode { get; set; }

        public int DeviceNumber
        {
            get
            {
                if (this.DeviceCode < EPLCDeviceCode.X || this.DeviceCode > EPLCDeviceCode.DY)
                    return this.m_DeviceNumber;
                int result;
                return this.m_DeviceHexNumber != null && int.TryParse(this.m_DeviceHexNumber, NumberStyles.HexNumber, (IFormatProvider)null, out result) ? result : 0;
            }
            set
            {
                this.m_DeviceHexNumber = value.ToString("X");
                this.m_DeviceNumber = value;
            }
        }

        public string DeviceHexNumber
        {
            get
            {
                return this.DeviceCode >= EPLCDeviceCode.X && this.DeviceCode <= EPLCDeviceCode.DY ? this.m_DeviceHexNumber : this.m_DeviceNumber.ToString("X");
            }
            set
            {
                this.m_DeviceHexNumber = value;
                int result;
                if (int.TryParse(this.m_DeviceHexNumber, NumberStyles.HexNumber, (IFormatProvider)null, out result))
                    this.m_DeviceNumber = result;
                else
                    this.m_DeviceNumber = 0;
            }
        }

        public ushort WordCount { get; set; }

        public EParseDataType DataType { get; set; }

        public string Value { get; set; }

        public SendCommand()
        {
            this.DeviceCode = EPLCDeviceCode.M;
            this.DataType = EParseDataType.Short;
            this.Value = string.Empty;
        }
    }
}