using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCCommunication_v2.Mitsubishi
{
    internal static class PLCConverter
    {
        internal static string Convert1BitStringFromBooleanData(bool boolData) => !boolData ? "0" : "1";

        internal static byte Convert1BitByteFromBooleanData(bool boolData)
        {
            return boolData ? (byte)16 : (byte)0;
        }

        internal static string ConvertNBitStringFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            string empty = string.Empty;
            foreach (bool flag in boolArr)
                empty += flag ? "1" : "0";
            return empty;
        }

        internal static byte[] ConvertNBitByteArrayFromBooleanArrayData(IEnumerable<bool> boolArr)
        {
            int num = boolArr.Count<bool>();
            byte[] numArray = num % 2 == 0 ? new byte[num / 2] : new byte[num / 2 + 1];
            for (int index = 0; index < num; index += 2)
                numArray[index / 2] = (byte)((boolArr.ElementAt<bool>(index) ? 16 : 0) + (index + 1 >= num || !boolArr.ElementAt<bool>(index + 1) ? 0 : 1));
            return numArray;
        }

        internal static byte Convert1ByteFromData(object val)
        {
            Type type = val.GetType();
            if (type == typeof(byte))
                return (byte)val;
            if (type == typeof(char))
                return (byte)(char)val;
            throw new Exception("Invalid data format.");
        }

        internal static string Convert1ByteStringFromData(object val)
        {
            return PLCConverter.Convert1ByteFromData(val).ToString("X2");
        }

        internal static byte[] Convert1WordByteArrayFromData(object val)
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
            if (type == typeof(string) && (val as string).Length <= 2)
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
            char[] chars = val is IEnumerable<char> source && source.Count<char>() <= 2 ? source.ToArray<char>() : throw new Exception("Invalid data format.");
            if (chars.Length % 2 == 0)
                return Encoding.ASCII.GetBytes(chars);
            byte[] numArray1 = new byte[chars.Length + 1];
            byte[] bytes1 = Encoding.ASCII.GetBytes(chars);
            for (int index = 0; index < bytes1.Length; ++index)
                numArray1[index] = bytes1[index];
            return numArray1;
        }

        internal static string Convert1WordStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.Convert1WordByteArrayFromData(val));
        }

        internal static byte[] Convert2WordsByteArrayFromData(object val)
        {
            Type type = val.GetType();
            if (type == typeof(int))
                return BitConverter.GetBytes((int)val);
            if (type == typeof(uint))
                return BitConverter.GetBytes((uint)val);
            if (type == typeof(float))
            {
                float result;
                float.TryParse(val.ToString(), out result);
                return BitConverter.GetBytes(result);
            }
            if (type == typeof(string) && (val as string).Length > 2 && (val as string).Length <= 4)
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
            char[] chars = val is IEnumerable<char> source && source.Count<char>() > 2 && source.Count<char>() <= 4 ? source.ToArray<char>() : throw new Exception("Invalid data format.");
            if (chars.Length % 2 == 0)
                return Encoding.ASCII.GetBytes(chars);
            byte[] numArray1 = new byte[chars.Length + 1];
            byte[] bytes1 = Encoding.ASCII.GetBytes(chars);
            for (int index = 0; index < bytes1.Length; ++index)
                numArray1[index] = bytes1[index];
            return numArray1;
        }

        internal static string Convert2WordsStringFromData(object val)
        {
            return PLCConverter.ConvertValueToString(PLCConverter.Convert2WordsByteArrayFromData(val), true);
        }

        internal static byte[][] Convert2WordsByteArrayFrom4WordsData(object val)
        {
            Type type = val.GetType();
            byte[] bytes;
            if (type == typeof(long))
                bytes = BitConverter.GetBytes((long)val);
            else if (type == typeof(ulong))
            {
                bytes = BitConverter.GetBytes((ulong)val);
            }
            else
            {
                if (!(type == typeof(double)))
                    throw new Exception("Invalid data format.");
                bytes = BitConverter.GetBytes((double)val);
            }
            return new byte[2][]
            {
        ((IEnumerable<byte>) bytes).Take<byte>(4).ToArray<byte>(),
        ((IEnumerable<byte>) bytes).Skip<byte>(4).Take<byte>(4).ToArray<byte>()
            };
        }

        internal static string[] Convert2WordsStringFrom4WordsData(object val)
        {
            byte[][] numArray = PLCConverter.Convert2WordsByteArrayFrom4WordsData(val);
            return new string[2]
            {
        PLCConverter.ConvertValueToString(numArray[0], true),
        PLCConverter.ConvertValueToString(numArray[1], true)
            };
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

        internal static string ConvertValueToString(byte[] byteArray, bool isDWord = false)
        {
            string str = string.Empty;
            if (isDWord)
            {
                for (int index = 0; index < byteArray.Length; index += 4)
                    str = str + (index + 3 < byteArray.Length ? byteArray[index + 3].ToString("X2") : "00") + (index + 2 < byteArray.Length ? byteArray[index + 2].ToString("X2") : "00") + (index + 1 < byteArray.Length ? byteArray[index + 1].ToString("X2") : "00") + byteArray[index].ToString("X2");
            }
            else
            {
                for (int index = 0; index < byteArray.Length; index += 2)
                    str = str + (index + 1 < byteArray.Length ? byteArray[index + 1].ToString("X2") : "00") + byteArray[index].ToString("X2");
            }
            return str;
        }

        internal static byte[] ConvertHexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length).Where<int>((Func<int, bool>)(x => x % 2 == 0)).Select<int, byte>((Func<int, byte>)(x => Convert.ToByte(hexString.Substring(x, 2), 16))).ToArray<byte>();
        }

        internal static string ConvertStringFromAddress(PLCSendingPacket data)
        {
            string str1 = data.DeviceCode.ToString();
            string empty = string.Empty;
            string str2 = data.DeviceCode > EPLCDeviceCode.DY || data.DeviceCode < EPLCDeviceCode.X ? data.Address.ToString("D6") : data.Address.ToString("X6");
            return (str1.Length == 1 ? str1 + "*" : str1) + (str2.Length > 6 ? str2.Substring(str2.Length - 6, 6) : str2);
        }

        internal static string ConvertStringFromAddress(PLCSendingPacket data, int offset)
        {
            string str1 = data.DeviceCode.ToString();
            string empty = string.Empty;
            string str2 = data.DeviceCode > EPLCDeviceCode.DY || data.DeviceCode < EPLCDeviceCode.X ? (data.Address + offset).ToString("D6") : (data.Address + offset).ToString("X6");
            return (str1.Length == 1 ? str1 + "*" : str1) + (str2.Length > 6 ? str2.Substring(str2.Length - 6, 6) : str2);
        }

        internal static byte[] ConvertByteArrayFromAddress(PLCSendingPacket data)
        {
            byte[] bytes = BitConverter.GetBytes(data.Address);
            bytes[bytes.Length - 1] = (byte)data.DeviceCode;
            return bytes;
        }

        internal static byte[] ConvertByteArrayFromAddress(PLCSendingPacket data, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(data.Address + offset);
            bytes[bytes.Length - 1] = (byte)data.DeviceCode;
            return bytes;
        }
    }
}

