using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCCommunication_v2.Mitsubishi
{
    public class PLCReceivingPacket : IPLCReceivingPacket
    {
        private byte[] m_OriginValueArray;

        public EPLCDeviceCode DeviceCode { get; }

        public int Address { get; }

        public PLCReceivingPacket(byte[] receivedData, EPLCDeviceCode code, int address)
        {
            this.m_OriginValueArray = ((IEnumerable<byte>)receivedData).ToArray<byte>();
            this.DeviceCode = code;
            this.Address = address;
        }

        public byte[] GetByteArray()
        {
            return this.m_OriginValueArray == null ? (byte[])null : ((IEnumerable<byte>)this.m_OriginValueArray).ToArray<byte>();
        }

        public bool[] GetBooleanArray()
        {
            if (this.m_OriginValueArray == null)
                return (bool[])null;
            List<bool> boolList = new List<bool>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 2)
            {
                if (startIndex + 1 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[2];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    foreach (char ch in Convert.ToString(BitConverter.ToInt16(numArray, 0), 2).PadLeft(16, '0').Reverse<char>())
                        boolList.Add(ch.Equals('1'));
                }
                else
                {
                    foreach (char ch in Convert.ToString(BitConverter.ToInt16(this.m_OriginValueArray, startIndex), 2).PadLeft(16, '0').Reverse<char>())
                        boolList.Add(ch.Equals('1'));
                }
            }
            return boolList.ToArray();
        }

        public short[] GetInt16Array()
        {
            if (this.m_OriginValueArray == null)
                return (short[])null;
            List<short> shortList = new List<short>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 2)
            {
                if (startIndex + 1 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[2];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    shortList.Add(BitConverter.ToInt16(numArray, 0));
                }
                else
                    shortList.Add(BitConverter.ToInt16(this.m_OriginValueArray, startIndex));
            }
            return shortList.ToArray();
        }

        public ushort[] GetUInt16Array()
        {
            if (this.m_OriginValueArray == null)
                return (ushort[])null;
            List<ushort> ushortList = new List<ushort>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 2)
            {
                if (startIndex + 1 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[2];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    ushortList.Add(BitConverter.ToUInt16(numArray, 0));
                }
                else
                    ushortList.Add(BitConverter.ToUInt16(this.m_OriginValueArray, startIndex));
            }
            return ushortList.ToArray();
        }

        public int[] GetInt32Array()
        {
            if (this.m_OriginValueArray == null)
                return (int[])null;
            List<int> intList = new List<int>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 4)
            {
                if (startIndex + 3 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[4];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    intList.Add(BitConverter.ToInt32(numArray, 0));
                }
                else
                    intList.Add(BitConverter.ToInt32(this.m_OriginValueArray, startIndex));
            }
            return intList.ToArray();
        }

        public uint[] GetUInt32Array()
        {
            if (this.m_OriginValueArray == null)
                return (uint[])null;
            List<uint> uintList = new List<uint>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 4)
            {
                if (startIndex + 3 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[4];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    uintList.Add(BitConverter.ToUInt32(numArray, 0));
                }
                else
                    uintList.Add(BitConverter.ToUInt32(this.m_OriginValueArray, startIndex));
            }
            return uintList.ToArray();
        }

        public long[] GetInt64Array()
        {
            if (this.m_OriginValueArray == null)
                return (long[])null;
            List<long> longList = new List<long>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 8)
            {
                if (startIndex + 7 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[8];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    longList.Add(BitConverter.ToInt64(numArray, 0));
                }
                else
                    longList.Add(BitConverter.ToInt64(this.m_OriginValueArray, startIndex));
            }
            return longList.ToArray();
        }

        public ulong[] GetUInt64Array()
        {
            if (this.m_OriginValueArray == null)
                return (ulong[])null;
            List<ulong> ulongList = new List<ulong>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 8)
            {
                if (startIndex + 7 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[8];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    ulongList.Add(BitConverter.ToUInt64(numArray, 0));
                }
                else
                    ulongList.Add(BitConverter.ToUInt64(this.m_OriginValueArray, startIndex));
            }
            return ulongList.ToArray();
        }

        public float[] GetSingleArray()
        {
            if (this.m_OriginValueArray == null)
                return (float[])null;
            List<float> floatList = new List<float>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 4)
            {
                if (startIndex + 3 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[4];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    floatList.Add(BitConverter.ToSingle(numArray, 0));
                }
                else
                    floatList.Add(BitConverter.ToSingle(this.m_OriginValueArray, startIndex));
            }
            return floatList.ToArray();
        }

        public double[] GetDoubleArray()
        {
            if (this.m_OriginValueArray == null)
                return (double[])null;
            List<double> doubleList = new List<double>();
            for (int startIndex = 0; startIndex < this.m_OriginValueArray.Length; startIndex += 8)
            {
                if (startIndex + 7 >= this.m_OriginValueArray.Length)
                {
                    int num = 0;
                    byte[] numArray = new byte[8];
                    while (startIndex < this.m_OriginValueArray.Length)
                        numArray[num++] = this.m_OriginValueArray[startIndex++];
                    doubleList.Add(BitConverter.ToDouble(numArray, 0));
                }
                else
                    doubleList.Add(BitConverter.ToDouble(this.m_OriginValueArray, startIndex));
            }
            return doubleList.ToArray();
        }

        public string GetASCIIString()
        {
            return this.m_OriginValueArray == null ? (string)null : Encoding.ASCII.GetString(this.m_OriginValueArray);
        }
    }
}
