using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PLCCommunication_v2.Panasonic
{
    internal static class PLCConverter
    {
        internal static int CalcWordCount(object val)
        {
            int num = Marshal.SizeOf(val);
            if (num == 0)
                return 0;
            return num % 2 != 0 ? num / 2 + 1 : num / 2;
        }

        internal static string EncodeBCC(string msg)
        {
            byte num = 0;
            foreach (char ch in msg)
                num ^= (byte)ch;
            return num.ToString("X2");
        }

        internal static bool DecodeBCC(string msg, string bcc)
        {
            byte num = 0;
            foreach (char ch in msg)
                num ^= (byte)ch;
            return num.ToString("X2").Equals(bcc);
        }

        internal static string Convert1BitStringFromBooleanData(bool boolData) => !boolData ? "0" : "1";

        internal static byte Convert1BitByteFromBooleanData(bool boolData)
        {
            return boolData ? (byte)1 : (byte)0;
        }

        internal static string ConvertNWordsStringFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            string str = string.Empty;
            for (int index = 0; index < boolArr.Count<bool>(); index += 16)
            {
                byte num1 = 0;
                byte num2 = 0;
                byte num3 = (byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)num1 + (boolArr.Count<bool>() < index ? (boolArr.ElementAt<bool>(index) ? 1 : 0) : 0)) + (boolArr.Count<bool>() < index + 1 ? (boolArr.ElementAt<bool>(index + 1) ? 2 : 0) : 0)) + (boolArr.Count<bool>() < index + 2 ? (boolArr.ElementAt<bool>(index + 2) ? 4 : 0) : 0)) + (boolArr.Count<bool>() < index + 3 ? (boolArr.ElementAt<bool>(index + 3) ? 8 : 0) : 0)) + (boolArr.Count<bool>() < index + 4 ? (boolArr.ElementAt<bool>(index + 4) ? 16 : 0) : 0)) + (boolArr.Count<bool>() < index + 5 ? (boolArr.ElementAt<bool>(index + 5) ? 32 : 0) : 0)) + (boolArr.Count<bool>() < index + 6 ? (boolArr.ElementAt<bool>(index + 6) ? 64 : 0) : 0)) + (boolArr.Count<bool>() < index + 7 ? (boolArr.ElementAt<bool>(index + 7) ? 128 : 0) : 0));
                num2 += boolArr.Count<bool>() < index + 8 ? (boolArr.ElementAt<bool>(index + 8) ? (byte)1 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 9 ? (boolArr.ElementAt<bool>(index + 9) ? (byte)2 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 10 ? (boolArr.ElementAt<bool>(index + 10) ? (byte)4 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 11 ? (boolArr.ElementAt<bool>(index + 11) ? (byte)8 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 12 ? (boolArr.ElementAt<bool>(index + 12) ? (byte)16 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 13 ? (boolArr.ElementAt<bool>(index + 13) ? (byte)32 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 14 ? (boolArr.ElementAt<bool>(index + 14) ? (byte)64 : (byte)0) : (byte)0;
                num2 += boolArr.Count<bool>() < index + 15 ? (boolArr.ElementAt<bool>(index + 15) ? (byte)128 : (byte)0) : (byte)0;
                str = str + num3.ToString("X2") + num2.ToString("X2");
            }
            if (boolArr.Count<bool>() <= 16)
                str += str;
            return str;
        }

        internal static byte[] ConvertNWordsByteArrayFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            int num1 = boolArr.Count<bool>();
            List<byte> byteList = new List<byte>();
            for (int index = 0; index < num1; index += 16)
            {
                byte num2 = 0;
                byte num3 = 0;
                byte num4 = (byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)num2 + (num1 < index ? (boolArr.ElementAt<bool>(index) ? 1 : 0) : 0)) + (num1 < index + 1 ? (boolArr.ElementAt<bool>(index + 1) ? 2 : 0) : 0)) + (num1 < index + 2 ? (boolArr.ElementAt<bool>(index + 2) ? 4 : 0) : 0)) + (num1 < index + 3 ? (boolArr.ElementAt<bool>(index + 3) ? 8 : 0) : 0)) + (num1 < index + 4 ? (boolArr.ElementAt<bool>(index + 4) ? 16 : 0) : 0)) + (num1 < index + 5 ? (boolArr.ElementAt<bool>(index + 5) ? 32 : 0) : 0)) + (num1 < index + 6 ? (boolArr.ElementAt<bool>(index + 6) ? 64 : 0) : 0)) + (num1 < index + 7 ? (boolArr.ElementAt<bool>(index + 7) ? 128 : 0) : 0));
                byte num5 = (byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)(byte)((int)num3 + (num1 < index + 8 ? (boolArr.ElementAt<bool>(index + 8) ? 1 : 0) : 0)) + (num1 < index + 9 ? (boolArr.ElementAt<bool>(index + 9) ? 2 : 0) : 0)) + (num1 < index + 10 ? (boolArr.ElementAt<bool>(index + 10) ? 4 : 0) : 0)) + (num1 < index + 11 ? (boolArr.ElementAt<bool>(index + 11) ? 8 : 0) : 0)) + (num1 < index + 12 ? (boolArr.ElementAt<bool>(index + 12) ? 16 : 0) : 0)) + (num1 < index + 13 ? (boolArr.ElementAt<bool>(index + 13) ? 32 : 0) : 0)) + (num1 < index + 14 ? (boolArr.ElementAt<bool>(index + 14) ? 64 : 0) : 0)) + (num1 < index + 15 ? (boolArr.ElementAt<bool>(index + 15) ? 128 : 0) : 0));
                byteList.Add(num4);
                byteList.Add(num5);
            }
            return byteList.ToArray();
        }

        internal static byte[] ConvertMultiWordsByteArrayFromData(object val)
        {
            Type type = val.GetType();
            if (type == typeof(byte))
                return new byte[2] { (byte)val, (byte)0 };
            if (type == typeof(char))
                return new byte[2] { (byte)(char)val, (byte)0 };
            if (type == typeof(short))
                return BitConverter.GetBytes((short)val);
            if (type == typeof(ushort))
                return BitConverter.GetBytes((ushort)val);
            if (type == typeof(int))
                return BitConverter.GetBytes((int)val);
            if (type == typeof(uint))
                return BitConverter.GetBytes((uint)val);
            if (type == typeof(long))
                return BitConverter.GetBytes((long)val);
            if (type == typeof(ulong))
                return BitConverter.GetBytes((ulong)val);
            if (type == typeof(float))
                return BitConverter.GetBytes((float)val);
            if (type == typeof(double))
                return BitConverter.GetBytes((double)val);
            if (type == typeof(string))
            {
                char[] array = ((string)val).ToArray<char>();
                if (array.Length % 2 == 0)
                    return Encoding.ASCII.GetBytes(array);
                byte[] numArray = new byte[array.Length + 1];
                byte[] bytes = Encoding.ASCII.GetBytes(array);
                for (int index = 0; index < bytes.Length; ++index)
                    numArray[index] = bytes[index];
                return numArray;
            }
            switch (val)
            {
                case IEnumerable<char> source1:
                    if (source1.Count<char>() % 2 == 0)
                        return Encoding.ASCII.GetBytes(source1.ToArray<char>());
                    byte[] numArray1 = new byte[source1.Count<char>() + 1];
                    byte[] bytes1 = Encoding.ASCII.GetBytes(source1.ToArray<char>());
                    for (int index = 0; index < bytes1.Length; ++index)
                        numArray1[index] = bytes1[index];
                    return numArray1;
                case IEnumerable<byte> source2:
                    if (source2.Count<byte>() % 2 == 0)
                        return source2.ToArray<byte>();
                    byte[] numArray2 = new byte[source2.Count<byte>() + 1];
                    byte[] array1 = source2.ToArray<byte>();
                    for (int index = 0; index < array1.Length; ++index)
                        numArray2[index] = array1[index];
                    return numArray2;
                default:
                    throw new Exception("Invalid data format.");
            }
        }

        internal static byte[] ConvertMultiWordsByteArrayFromDataList(IEnumerable vals)
        {
            List<byte> byteList = new List<byte>();
            foreach (object val in vals)
                byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            return byteList.ToArray();
        }

        internal static string ConvertMultiWordsStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
        }

        internal static string ConvertMultiWordsStringFromDataList(IEnumerable vals)
        {
            string empty = string.Empty;
            foreach (object val in vals)
                empty += PLCConverter.ConvertValueToString(PLCConverter.ConvertMultiWordsByteArrayFromData(val));
            return empty;
        }

        internal static string ConvertValueToString(byte[] byteArray)
        {
            string empty = string.Empty;
            for (int index = 0; index < byteArray.Length; ++index)
                empty += byteArray[index].ToString("X2");
            return empty;
        }

        internal static byte[] ConvertHexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length).Where<int>((Func<int, bool>)(x => x % 2 == 0)).Select<int, byte>((Func<int, byte>)(x => Convert.ToByte(hexString.Substring(x, 2), 16))).ToArray<byte>();
        }

        internal static string ConvertStringFromAddress(PLCSendingPacket data)
        {
            EPLCDeviceCode eplcDeviceCode = data.DeviceCode >= EPLCDeviceCode.T && (data.DeviceCode <= EPLCDeviceCode.C_L || data.DeviceCode >= EPLCDeviceCode.IX) ? data.DeviceCode : throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            string str1;
            if (!eplcDeviceCode.ToString().Contains("_"))
            {
                eplcDeviceCode = data.DeviceCode;
                str1 = eplcDeviceCode.ToString();
            }
            else
            {
                eplcDeviceCode = data.DeviceCode;
                str1 = ((IEnumerable<string>)eplcDeviceCode.ToString().Split('_')).Last<string>();
            }
            string empty = string.Empty;
            string str2;
            if (data.DeviceCode < EPLCDeviceCode.L)
            {
                if (data.IsContact)
                {
                    string contactAddress = data.ContactAddress;
                    str2 = contactAddress.Substring(contactAddress.Length - 4, 4);
                }
                else
                {
                    string str3 = data.Address.ToString("D4");
                    str2 = str3.Substring(str3.Length - 4, 4);
                }
            }
            else
                str2 = "000000000";
            string str4 = str2;
            return str1 + str4;
        }

        internal static string ConvertStringFromAddress(PLCSendingPacket data, int offset)
        {
            EPLCDeviceCode eplcDeviceCode = data.DeviceCode >= EPLCDeviceCode.T && data.DeviceCode < EPLCDeviceCode.IX ? data.DeviceCode : throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            string str1;
            if (!eplcDeviceCode.ToString().Contains("_"))
            {
                eplcDeviceCode = data.DeviceCode;
                str1 = eplcDeviceCode.ToString();
            }
            else
            {
                eplcDeviceCode = data.DeviceCode;
                str1 = ((IEnumerable<string>)eplcDeviceCode.ToString().Split('_')).Last<string>();
            }
            string empty = string.Empty;
            string str2;
            if (data.DeviceCode < EPLCDeviceCode.L)
            {
                string str3 = data.IsContact ? data.ContactAddress : data.Address.ToString("D4");
                str2 = str3.Substring(str3.Length - 4, 4);
            }
            else
            {
                if (offset <= 0)
                    throw new Exception("Invalid offset value.");
                int num = data.Address;
                string str4 = num.ToString("D5");
                string str5 = str4.Substring(str4.Length - 5, 5);
                num = data.Address + offset - 1;
                string str6 = num.ToString("D5");
                string str7 = str6.Substring(str6.Length - 5, 5);
                str2 = str5 + str7;
            }
            string str8 = str2;
            return str1 + str8;
        }

        internal static byte[] ConvertByteArrayFromContactAddress(PLCSendingPacket data)
        {
            if (data.DeviceCode >= EPLCDeviceCode.T)
                throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            List<byte> byteList = new List<byte>();
            byteList.Add((byte)data.DeviceCode);
            byteList.AddRange(((IEnumerable<byte>)BitConverter.GetBytes(data.Address)).Take<byte>(2));
            byte result;
            byteList.Add(byte.TryParse(data.ContactAddress.Substring(data.ContactAddress.Length - 1, 1), out result) ? result : (byte)0);
            return byteList.ToArray();
        }

        internal static byte[] ConvertByteArrayFromDataAddress(PLCSendingPacket data, int offset)
        {
            if (data.DeviceCode >= EPLCDeviceCode.T)
                throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            if (offset <= 0)
                throw new Exception("Invalid offset value.");
            List<byte> byteList = new List<byte>();
            byteList.Add((byte)data.DeviceCode);
            byteList.AddRange(((IEnumerable<byte>)BitConverter.GetBytes(data.Address)).Take<byte>(2));
            byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes((short)offset));
            return byteList.ToArray();
        }
    }
}
