using System;
using System.Globalization;

namespace PLCCommunication_v2.Panasonic
{
    public class PLCSendingPacket : IPLCSendingPacket
    {
        public EPLCDeviceCode DeviceCode { get; }

        public int Address { get; }

        public string ContactAddress { get; }

        public object Value { get; }

        public bool IsContact { get; }

        public bool IsRead { get; }

        public ushort WordCount { get; }

        public PLCSendingPacket(
          EPLCDeviceCode code,
          string address,
          bool isContact,
          bool isRead,
          ushort readWordCount)
        {
            this.DeviceCode = code;
            this.IsContact = isContact;
            if (isContact)
            {
                int result;
                if (int.TryParse(address.Substring(0, address.Length - 1), out result) && int.TryParse(address.Substring(address.Length - 1, 1), NumberStyles.HexNumber, (IFormatProvider)null, out int _))
                {
                    string str = string.Empty;
                    if (address.Length < 4)
                        str = "0000" + address;
                    this.ContactAddress = str.Substring(str.Length - 4, 4);
                    this.Address = result;
                }
                else
                {
                    this.ContactAddress = "0000";
                    this.Address = 0;
                }
            }
            else
            {
                int result;
                if (int.TryParse(address, out result))
                {
                    this.Address = result;
                    this.ContactAddress = address;
                }
                else
                {
                    this.Address = 0;
                    this.ContactAddress = "0000";
                }
            }
            this.Value = (object)(byte)0;
            this.WordCount = readWordCount;
            this.IsRead = true;
        }

        public PLCSendingPacket(EPLCDeviceCode code, string address, bool isContact, object value)
        {
            this.DeviceCode = code;
            this.IsContact = isContact;
            if (isContact)
            {
                int result;
                if (int.TryParse(address.Substring(0, address.Length - 1), out result) && int.TryParse(address.Substring(address.Length - 1, 1), NumberStyles.HexNumber, (IFormatProvider)null, out int _))
                {
                    string str = string.Empty;
                    if (address.Length < 4)
                        str = "0000" + address;
                    this.ContactAddress = str.Substring(str.Length - 4, 4);
                    this.Address = result;
                }
                else
                {
                    this.ContactAddress = "0000";
                    this.Address = 0;
                }
            }
            else
            {
                int result;
                if (int.TryParse(address, out result))
                {
                    this.Address = result;
                    this.ContactAddress = address;
                }
                else
                {
                    this.Address = 0;
                    this.ContactAddress = "0000";
                }
            }
            this.Value = value;
            this.WordCount = (ushort)0;
            this.IsRead = false;
        }
    }
}
