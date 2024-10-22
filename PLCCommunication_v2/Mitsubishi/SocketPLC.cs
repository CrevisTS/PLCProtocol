using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;

namespace PLCCommunication_v2.Mitsubishi
{
    public class SocketPLC : IPLC, IDisposable
    {
        private readonly string _DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Check Box";
        private readonly object _CommunicationLock = new object();
        private TcpClient m_Client;
        private NetworkStream m_Stream;
        private SocketSetting m_Setting;
        private Thread m_ConnectionCheckThread;
        private Thread m_ReadThread;
        private byte[] m_CurrentData;
        private AutoResetEvent _currentDataOnAutoReset = new AutoResetEvent(false);
        public string IP
        {
            get => this.m_Setting == null ? string.Empty : this.m_Setting.IP;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.IP = value;
            }
        }

        public int PortNumber
        {
            get => this.m_Setting != null ? this.m_Setting.PortNumber : 0;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.PortNumber = value;
            }
        }

        public byte PCNo
        {
            get => this.m_Setting != null ? this.m_Setting.PCNo : byte.MaxValue;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.PCNo = value;
            }
        }

        public byte NetworkNo
        {
            get => this.m_Setting != null ? this.m_Setting.NetworkNo : (byte)0;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.NetworkNo = value;
            }
        }

        public EPLCProtocolFormat ProtocolFormat
        {
            get => this.m_Setting != null ? this.m_Setting.ProtocolFormat : EPLCProtocolFormat.Binary;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.ProtocolFormat = value;
            }
        }

        public uint Timeout
        {
            get => this.m_Setting != null ? this.m_Setting.Timeout * 250U : 0U;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.Timeout = value / 250U;
            }
        }

        public ushort ReconnectCount
        {
            get => this.m_Setting != null ? this.m_Setting.ReconnectCount : (ushort)0;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.ReconnectCount = value;
            }
        }

        public bool IsConnected
        {
            get => !this.IP.Equals(string.Empty) && this.m_Client != null && this.m_Client.Connected;
        }

        public SocketPLC()
        {
            this.m_Setting = new SocketSetting();
            this.IP = "192.168.10.100";
            this.PortNumber = 6000;
            this.ProtocolFormat = EPLCProtocolFormat.Binary;
            this.NetworkNo = (byte)0;
            this.PCNo = byte.MaxValue;
            this.Timeout = 1000U;
        }

        public SocketPLC(
          string ipAddress,
          int portNum,
          EPLCProtocolFormat protocolFormat = EPLCProtocolFormat.Binary,
          byte networkNo = 0,
          byte pcNo = 255,
          uint timeout = 4000)
        {
            this.m_Setting = new SocketSetting();
            this.IP = ipAddress;
            this.PortNumber = portNum;
            this.ProtocolFormat = protocolFormat;
            this.NetworkNo = networkNo;
            this.PCNo = pcNo;
            this.Timeout = timeout;
        }

        public void Dispose() => this.Disconnect();

        public void Connect()
        {
            if (this.IsConnected)
                throw new Exception("Already connected.");
            this.TcpConnect();
            this.m_ConnectionCheckThread = new Thread(new ThreadStart(this.OnCheckProcess))
            {
                Name = "Socket_ConnectionCheck"
            };
            this.m_ConnectionCheckThread.Start();
        }

        public void Connect(string ip, int portNum)
        {
            this.IP = ip;
            this.PortNumber = portNum;
            this.Connect();
        }

        private void TcpConnect()
        {
            this.m_Client = new TcpClient();
            this.m_Client.Connect(this.IP, this.PortNumber);
            this.m_Stream = this.m_Client.GetStream();
            if (this.m_ReadThread != null && this.m_ReadThread.IsAlive)
                return;
            this.m_ReadThread = new Thread((ThreadStart)(() =>
            {
                try
                {
                    byte[] numArray = new byte[256];
                    int num = 0;
                    int count;
                    while ((count = this.m_Stream.Read(numArray, 0, numArray.Length)) != 0)
                    {
                        if (count == numArray.Length)
                        {
                            List<byte> byteList = new List<byte>((IEnumerable<byte>)numArray);
                            while (count == numArray.Length)
                            {
                                count = this.m_Stream.Read(numArray, 0, numArray.Length);
                                byteList.AddRange(((IEnumerable<byte>)numArray).Take<byte>(count));
                            }
                            this.m_CurrentData = byteList.ToArray();
                            byteList.Clear();
                        }
                        else
                            this.m_CurrentData = ((IEnumerable<byte>)numArray).Take<byte>(count).ToArray<byte>();
                        numArray = new byte[256];
                        num = 0;
                        _currentDataOnAutoReset.Set();
                    }
                }
                catch (ThreadAbortException ex)
                {
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.Message);
                }
                finally
                {
                    this.m_CurrentData = new byte[0];
                    if (this.m_Stream != null)
                    {
                        this.m_Stream.Flush();
                        this.m_Stream.Close(500);
                        this.m_Stream = (NetworkStream)null;
                    }
                    if (this.m_Client != null)
                    {
                        this.m_Client.Close();
                        this.m_Client = (TcpClient)null;
                    }
                }
            }))
            {
                Name = "Socket_DataRead"
            };
            this.m_ReadThread.Start();
        }

        public void Disconnect()
        {
            if (this.m_ConnectionCheckThread != null && this.m_ConnectionCheckThread.IsAlive)
            {
                this.m_ConnectionCheckThread.Abort();
                this.m_ConnectionCheckThread.Join(1000);
            }
            this.TcpDisconnect();
        }

        private void TcpDisconnect()
        {
            if (this.m_ReadThread != null && this.m_ReadThread.IsAlive)
            {
                this.m_ReadThread.Abort();
                this.m_ReadThread.Join(1000);
            }
            if (this.m_Stream != null)
            {
                this.m_Stream.Flush();
                this.m_Stream.Close(500);
                this.m_Stream = (NetworkStream)null;
            }
            if (this.m_Client == null)
                return;
            this.m_Client.Close();
            this.m_Client = (TcpClient)null;
        }

        public void Refresh()
        {
            this.TcpDisconnect();
            this.TcpConnect();
        }

        private void OnCheckProcess()
        {
            try
            {
                int num = 0;
                while (true)
                {
                    while (!this.IsConnected)
                    {
                        try
                        {
                            this.Refresh();
                        }
                        catch (ThreadAbortException ex)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            if (this.ReconnectCount != (ushort)0)
                            {
                                if (num > (int)this.ReconnectCount)
                                    throw new Exception("PLC reconnection failed : " + ex.Message + Environment.NewLine + "Please check LAN cable or PLC power.");
                                ++num;
                                Thread.Sleep(100);
                                continue;
                            }
                            continue;
                        }
                        num = 0;
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException ex)
            {
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message);
            }
        }

        public void Load()
        {
            if (!File.Exists(this._DefaultPath + "\\Mitsubishi_Socket.xml"))
                this.Save();
            using (StreamReader streamReader = new StreamReader(this._DefaultPath + "\\Mitsubishi_Socket.xml"))
            {
                if (!(new XmlSerializer(typeof(SocketSetting)).Deserialize((TextReader)streamReader) is SocketSetting socketSetting))
                    socketSetting = this.m_Setting;
                this.m_Setting = socketSetting;
            }
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                this.Save(filePath);
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                if (!(new XmlSerializer(typeof(SocketSetting)).Deserialize((TextReader)streamReader) is SocketSetting socketSetting))
                    socketSetting = this.m_Setting;
                this.m_Setting = socketSetting;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(this._DefaultPath);
            using (StreamWriter streamWriter = new StreamWriter(this._DefaultPath + "\\Mitsubishi_Socket.xml"))
                new XmlSerializer(typeof(SocketSetting)).Serialize((TextWriter)streamWriter, (object)(this.m_Setting ?? new SocketSetting()));
        }

        public void Save(string filePath)
        {
            if (!new FileInfo(filePath).Exists)
            {
                string[] strArray = filePath.Split('\\');
                string path = string.Empty;
                for (int index = 0; index < strArray.Length - 1; ++index)
                    path = path + strArray[index] + "\\";
                Directory.CreateDirectory(path);
            }
            using (StreamWriter streamWriter = new StreamWriter(filePath))
                new XmlSerializer(typeof(SocketSetting)).Serialize((TextWriter)streamWriter, (object)(this.m_Setting ?? new SocketSetting()));
        }

        public void Write(PLCSendingPacket data)
        {
            if (!this.IsConnected)
                throw new Exception("PLC not opened.");
            if (data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use write type message.");
            this.SendMsg(data);
        }

        public void Write(IEnumerable<PLCSendingPacket> dataArr)
        {
            if (!this.IsConnected)
                throw new Exception("PLC not opened.");
            if (dataArr.Any<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(data => data.IsRead)))
                throw new Exception("Wrong PLC massage type : Must use write type messages.");
            this.SendMsg(dataArr);
        }

        public void Read(PLCSendingPacket data, ref PLCReceivingPacket receiveValue)
        {
            if (!this.IsConnected)
                throw new Exception("PLC not opened.");
            if (!data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use read type message.");
            this.SendMsg(data, ref receiveValue);
        }

        public void Read(
          IEnumerable<PLCSendingPacket> dataArr,
          ref List<PLCReceivingPacket> receiveValueList)
        {
            if (!this.IsConnected)
                throw new Exception("PLC not opened.");
            if (dataArr.Any<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(data => !data.IsRead)))
                throw new Exception("Wrong PLC massage type : Must use read type messages.");
            this.SendMsg(dataArr, ref receiveValueList);
        }

        private void SendMsg(PLCSendingPacket data)
        {
            byte[] buffer = (byte[])null;
            Type type = data.Value.GetType();
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList = new List<byte>();
                    byteList.Add((byte)80);
                    byteList.Add((byte)0);
                    byteList.Add(this.NetworkNo);
                    byteList.Add(this.PCNo);
                    byteList.Add(byte.MaxValue);
                    byteList.Add((byte)3);
                    byteList.Add((byte)0);
                    List<byte> collection1 = new List<byte>(((IEnumerable<byte>)BitConverter.GetBytes(this.m_Setting.Timeout)).Take<byte>(2))
          {
            (byte) 1,
            (byte) 20
          };
                    ushort count;
                    if (data.Value is IEnumerable<bool> || type == typeof(bool))
                    {
                        collection1.Add((byte)1);
                        collection1.Add((byte)0);
                        collection1.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                        if (data.Value.GetType() == typeof(bool))
                        {
                            collection1.Add((byte)1);
                            collection1.Add((byte)0);
                            collection1.Add(PLCConverter.Convert1BitByteFromBooleanData((bool)data.Value));
                        }
                        else
                        {
                            byte[] collection2 = PLCConverter.ConvertNBitByteArrayFromBooleanArrayData(data.Value as IEnumerable<bool>);
                            ushort num = (ushort)(data.Value as IEnumerable<bool>).Count<bool>();
                            collection1.AddRange((IEnumerable<byte>)BitConverter.GetBytes(num));
                            collection1.AddRange((IEnumerable<byte>)collection2);
                        }
                        count = (ushort)collection1.Count;
                    }
                    else
                    {
                        collection1.Add((byte)0);
                        collection1.Add((byte)0);
                        collection1.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                        byte[] collection3 = !(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? PLCConverter.ConvertMultiWordsByteArrayFromData(data.Value) : PLCConverter.ConvertMultiWordsByteArrayFromDataList(vals);
                        ushort num = (ushort)(collection3.Length / 2);
                        collection1.AddRange((IEnumerable<byte>)BitConverter.GetBytes(num));
                        collection1.AddRange((IEnumerable<byte>)collection3);
                        count = (ushort)collection1.Count;
                    }
                    byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes(count));
                    byteList.AddRange((IEnumerable<byte>)collection1);
                    buffer = byteList.ToArray();
                    byteList.Clear();
                    collection1.Clear();
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str1 = "5000" + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF00";
                    string str2 = this.m_Setting.Timeout.ToString("X4") + "1401";
                    string str3;
                    ushort length1;
                    if (data.Value is IEnumerable<bool> || type == typeof(bool))
                    {
                        string str4 = str2 + "0001" + PLCConverter.ConvertStringFromAddress(data);
                        if (data.Value.GetType() == typeof(bool))
                        {
                            str3 = str4 + "0001" + PLCConverter.Convert1BitStringFromBooleanData((bool)data.Value);
                        }
                        else
                        {
                            string str5 = PLCConverter.ConvertNBitStringFromBooleanArrayData(data.Value as IEnumerable<bool>);
                            ushort length2 = (ushort)str5.Length;
                            str3 = str4 + length2.ToString("X4") + str5;
                        }
                        length1 = (ushort)str3.Length;
                    }
                    else
                    {
                        string str6 = str2 + "0000" + PLCConverter.ConvertStringFromAddress(data);
                        string empty = string.Empty;
                        string str7 = !(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? PLCConverter.ConvertMultiWordsStringFromData(data.Value) : PLCConverter.ConvertMultiWordsStringFromDataList(vals);
                        ushort num = (ushort)(str7.Length / 4);
                        str3 = str6 + num.ToString("X4") + str7;
                        length1 = (ushort)str3.Length;
                    }
                    buffer = Encoding.ASCII.GetBytes(str1 + length1.ToString("X4") + str3);
                    break;
            }
            lock (this._CommunicationLock)
            {
                this.m_Stream.Write(buffer, 0, buffer.Length);
                this.ReceiveMsg();
            }
        }

        private void SendMsg(PLCSendingPacket data, ref PLCReceivingPacket receiveData)
        {
            byte[] buffer = (byte[])null;
            byte[] receivedData = (byte[])null;
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList = new List<byte>();
                    byteList.Add((byte)80);
                    byteList.Add((byte)0);
                    byteList.Add(this.NetworkNo);
                    byteList.Add(this.PCNo);
                    byteList.Add(byte.MaxValue);
                    byteList.Add((byte)3);
                    byteList.Add((byte)0);
                    List<byte> collection = new List<byte>(((IEnumerable<byte>)BitConverter.GetBytes(this.m_Setting.Timeout)).Take<byte>(2))
          {
            (byte) 1,
            (byte) 4,
            (byte) 0,
            (byte) 0
          };
                    collection.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                    collection.AddRange((IEnumerable<byte>)BitConverter.GetBytes(data.WordCount));
                    byteList.AddRange((IEnumerable<byte>)BitConverter.GetBytes((ushort)collection.Count));
                    byteList.AddRange((IEnumerable<byte>)collection);
                    buffer = byteList.ToArray();
                    byteList.Clear();
                    collection.Clear();
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str1 = "5000" + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF00";
                    string str2 = this.m_Setting.Timeout.ToString("X4") + "04010000" + PLCConverter.ConvertStringFromAddress(data) + data.WordCount.ToString("X4");
                    ushort length = (ushort)str2.Length;
                    buffer = Encoding.ASCII.GetBytes(str1 + length.ToString("X4") + str2);
                    break;
            }
            lock (this._CommunicationLock)
            {
                this.m_Stream.Write(buffer, 0, buffer.Length);
                byte[] msg = this.ReceiveMsg(data.WordCount);
                switch (this.ProtocolFormat)
                {
                    case EPLCProtocolFormat.Binary:
                        receivedData = msg;
                        break;
                    case EPLCProtocolFormat.ASCII:
                        receivedData = new byte[msg.Length];
                        for (int index = 0; index < msg.Length; index += 2)
                        {
                            if (index + 1 < msg.Length)
                            {
                                receivedData[index + 1] = msg[index];
                                receivedData[index] = msg[index + 1];
                            }
                            else if (index + 1 == msg.Length)
                                receivedData[index] = msg[index];
                        }
                        break;
                }
                receiveData = new PLCReceivingPacket(receivedData, data.DeviceCode, data.Address);
            }
        }

        private void SendMsg(IEnumerable<PLCSendingPacket> dataList)
        {
            byte[] buffer = (byte[])null;
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            List<byte> byteList1 = (List<byte>)null;
            List<byte> byteList2 = (List<byte>)null;
            IEnumerable<PLCSendingPacket> source1 = dataList.Where<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(item => item.Value is IEnumerable<bool> || item.Value is bool));
            IEnumerable<PLCSendingPacket> source2 = dataList.Where<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(item => !(item.Value is IEnumerable<bool>) && !(item.Value is bool)));
            if (source1.Count<PLCSendingPacket>() > 0)
            {
                byte num1 = 0;
                switch (this.ProtocolFormat)
                {
                    case EPLCProtocolFormat.Binary:
                        List<byte> byteList3 = new List<byte>()
            {
              (byte) 80,
              (byte) 0,
              this.NetworkNo,
              this.PCNo,
              byte.MaxValue,
              (byte) 3,
              (byte) 0
            };
                        List<byte> collection1 = new List<byte>(((IEnumerable<byte>)BitConverter.GetBytes(this.m_Setting.Timeout)).Take<byte>(2))
            {
              (byte) 2,
              (byte) 20,
              (byte) 1,
              (byte) 0
            };
                        List<byte> collection2 = new List<byte>();
                        foreach (PLCSendingPacket data in source1)
                        {
                            if (data.Value is IEnumerable<bool> source3)
                            {
                                int num2 = source3.Count<bool>();
                                for (int index = 0; index < num2; ++index)
                                {
                                    collection2.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, index));
                                    collection2.Add((byte)data.DeviceCode);
                                    collection2.Add(source3.ElementAt<bool>(index) ? (byte)1 : (byte)0);
                                    ++num1;
                                }
                            }
                            else if (data.Value is bool flag)
                            {
                                collection2.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                                collection2.Add((byte)data.DeviceCode);
                                collection2.Add(flag ? (byte)1 : (byte)0);
                                ++num1;
                            }
                        }
                        collection1.AddRange((IEnumerable<byte>)collection2);
                        ushort num3 = collection1.Count % 2 == 0 ? (ushort)(collection1.Count / 2) : (ushort)(collection1.Count / 2 + 1);
                        byteList3.AddRange((IEnumerable<byte>)BitConverter.GetBytes(num3));
                        byteList3.AddRange((IEnumerable<byte>)collection1);
                        buffer = byteList3.ToArray();
                        byteList3.Clear();
                        collection1.Clear();
                        collection2.Clear();
                        byteList1 = (List<byte>)null;
                        byteList2 = (List<byte>)null;
                        break;
                    case EPLCProtocolFormat.ASCII:
                        string str1 = "5000" + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF00";
                        string str2 = this.m_Setting.Timeout.ToString("X4") + "14020001";
                        string str3 = string.Empty;
                        foreach (PLCSendingPacket data in source1)
                        {
                            if (data.Value is IEnumerable<bool> source4)
                            {
                                int num4 = source4.Count<bool>();
                                for (int index = 0; index < num4; ++index)
                                {
                                    str3 = str3 + PLCConverter.ConvertStringFromAddress(data, index) + (source4.ElementAt<bool>(index) ? "01" : "00");
                                    ++num1;
                                }
                            }
                            else if (data.Value is bool flag)
                            {
                                str3 = str3 + PLCConverter.ConvertStringFromAddress(data) + (flag ? "01" : "00");
                                ++num1;
                            }
                        }
                        string str4 = str2 + num1.ToString("X2") + str3;
                        ushort length = (ushort)str4.Length;
                        buffer = Encoding.ASCII.GetBytes(str1 + length.ToString("X4") + str4);
                        break;
                }
                lock (this._CommunicationLock)
                {
                    this.m_Stream.Write(buffer, 0, buffer.Length);
                    this.ReceiveMsg();
                }
            }
            if (source2.Count<PLCSendingPacket>() <= 0)
                return;
            byte num5 = 0;
            byte num6 = 0;
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList4 = new List<byte>()
          {
            (byte) 80,
            (byte) 0,
            this.NetworkNo,
            this.PCNo,
            byte.MaxValue,
            (byte) 3,
            (byte) 0
          };
                    List<byte> collection3 = new List<byte>((IEnumerable<byte>)BitConverter.GetBytes(this.m_Setting.Timeout))
          {
            (byte) 2,
            (byte) 20,
            (byte) 0,
            (byte) 0
          };
                    List<byte> collection4 = new List<byte>();
                    List<byte> collection5 = new List<byte>();
                    foreach (PLCSendingPacket data in source2)
                    {
                        if (data.Value is IEnumerable val)
                        {
                            int offset1 = 0;
                            switch (val)
                            {
                                case string _:
                                case IEnumerable<char> _:
                                    IEnumerable<byte> bytes = (IEnumerable<byte>)PLCConverter.ConvertMultiWordsByteArrayFromData((object)val);
                                    while (bytes.Count<byte>() >= 4)
                                    {
                                        collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset1 * 2));
                                        collection4.AddRange(bytes.Take<byte>(4));
                                        bytes = bytes.Skip<byte>(4);
                                        ++num6;
                                        ++offset1;
                                    }
                                    if (bytes.Count<byte>() > 0)
                                    {
                                        collection5.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset1 * 2));
                                        collection5.AddRange(bytes);
                                        ++num5;
                                        continue;
                                    }
                                    continue;
                                default:
                                    IEnumerator enumerator = val.GetEnumerator();
                                    try
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            object current = enumerator.Current;
                                            if (!(current.GetType() == typeof(long)) && !(current.GetType() == typeof(ulong)))
                                            {
                                                if (!(current.GetType() == typeof(double)))
                                                {
                                                    try
                                                    {
                                                        IEnumerable<byte> collection6 = (IEnumerable<byte>)PLCConverter.Convert2WordsByteArrayFromData(current);
                                                        collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset1));
                                                        collection4.AddRange(collection6);
                                                        offset1 += 2;
                                                        ++num6;
                                                        continue;
                                                    }
                                                    catch
                                                    {
                                                        IEnumerable<byte> collection7 = (IEnumerable<byte>)PLCConverter.Convert1WordByteArrayFromData(current);
                                                        collection5.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset1));
                                                        collection5.AddRange(collection7);
                                                        ++offset1;
                                                        ++num5;
                                                        continue;
                                                    }
                                                }
                                            }
                                            byte[][] numArray = PLCConverter.Convert2WordsByteArrayFrom4WordsData(current);
                                            collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset1));
                                            collection4.AddRange((IEnumerable<byte>)numArray[0]);
                                            int offset2 = offset1 + 2;
                                            collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, offset2));
                                            collection4.AddRange((IEnumerable<byte>)numArray[1]);
                                            offset1 = offset2 + 2;
                                            num6 += (byte)2;
                                        }
                                        continue;
                                    }
                                    finally
                                    {
                                        if (enumerator is IDisposable disposable)
                                            disposable.Dispose();
                                    }
                            }
                        }
                        else
                        {
                            if (!(data.Value.GetType() == typeof(long)) && !(data.Value.GetType() == typeof(ulong)))
                            {
                                if (!(data.Value.GetType() == typeof(double)))
                                {
                                    try
                                    {
                                        IEnumerable<byte> collection8 = (IEnumerable<byte>)PLCConverter.Convert2WordsByteArrayFromData(data.Value);
                                        collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                                        collection4.AddRange(collection8);
                                        ++num6;
                                        continue;
                                    }
                                    catch
                                    {
                                        IEnumerable<byte> collection9 = (IEnumerable<byte>)PLCConverter.Convert1WordByteArrayFromData(data.Value);
                                        collection5.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                                        collection5.AddRange(collection9);
                                        ++num5;
                                        continue;
                                    }
                                }
                            }
                            byte[][] numArray = PLCConverter.Convert2WordsByteArrayFrom4WordsData(data.Value);
                            collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data));
                            collection4.AddRange((IEnumerable<byte>)numArray[0]);
                            collection4.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, 2));
                            collection4.AddRange((IEnumerable<byte>)numArray[1]);
                            num6 += (byte)2;
                        }
                    }
                    collection3.Add(num5);
                    collection3.Add(num6);
                    collection3.AddRange((IEnumerable<byte>)collection5);
                    collection3.AddRange((IEnumerable<byte>)collection4);
                    ushort count = (ushort)collection3.Count;
                    byteList4.AddRange((IEnumerable<byte>)BitConverter.GetBytes(count));
                    byteList4.AddRange((IEnumerable<byte>)collection3);
                    buffer = byteList4.ToArray();
                    byteList4.Clear();
                    collection3.Clear();
                    collection4.Clear();
                    collection5.Clear();
                    byteList1 = (List<byte>)null;
                    byteList2 = (List<byte>)null;
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str5 = "5000" + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF00";
                    string str6 = this.m_Setting.Timeout.ToString("X4") + "14020000";
                    string str7 = string.Empty;
                    string str8 = string.Empty;
                    foreach (PLCSendingPacket data in source2)
                    {
                        string empty3 = string.Empty;
                        if (data.Value is IEnumerable val)
                        {
                            int offset3 = 0;
                            switch (val)
                            {
                                case string _:
                                case IEnumerable<char> _:
                                    string str9 = PLCConverter.ConvertMultiWordsStringFromData((object)val);
                                    while (str9.Length >= 8)
                                    {
                                        str8 = str8 + PLCConverter.ConvertStringFromAddress(data, offset3 * 2) + str9.Substring(4, 4) + str9.Substring(0, 4);
                                        str9 = str9.Substring(8);
                                        ++num6;
                                        ++offset3;
                                    }
                                    if (str9.Length > 0)
                                    {
                                        str7 = str7 + PLCConverter.ConvertStringFromAddress(data, offset3 * 2) + str9;
                                        ++num5;
                                        continue;
                                    }
                                    continue;
                                default:
                                    IEnumerator enumerator = val.GetEnumerator();
                                    try
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            object current = enumerator.Current;
                                            if (!(current.GetType() == typeof(long)) && !(current.GetType() == typeof(ulong)))
                                            {
                                                if (!(current.GetType() == typeof(double)))
                                                {
                                                    try
                                                    {
                                                        string str10 = PLCConverter.Convert2WordsStringFromData(current);
                                                        str8 = str8 + PLCConverter.ConvertStringFromAddress(data, offset3) + str10;
                                                        offset3 += 2;
                                                        ++num6;
                                                        continue;
                                                    }
                                                    catch
                                                    {
                                                        string str11 = PLCConverter.Convert1WordStringFromData(current);
                                                        str7 = str7 + PLCConverter.ConvertStringFromAddress(data, offset3) + str11;
                                                        ++offset3;
                                                        ++num5;
                                                        continue;
                                                    }
                                                }
                                            }
                                            string[] strArray = PLCConverter.Convert2WordsStringFrom4WordsData(current);
                                            str8 += PLCConverter.ConvertStringFromAddress(data, offset3);
                                            str8 += strArray[0];
                                            int offset4 = offset3 + 2;
                                            str8 += PLCConverter.ConvertStringFromAddress(data, offset4);
                                            str8 += strArray[1];
                                            offset3 = offset4 + 2;
                                            num6 += (byte)2;
                                        }
                                        continue;
                                    }
                                    finally
                                    {
                                        if (enumerator is IDisposable disposable)
                                            disposable.Dispose();
                                    }
                            }
                        }
                        else
                        {
                            if (!(data.Value.GetType() == typeof(long)) && !(data.Value.GetType() == typeof(ulong)))
                            {
                                if (!(data.Value.GetType() == typeof(double)))
                                {
                                    try
                                    {
                                        string str12 = PLCConverter.Convert2WordsStringFromData(data.Value);
                                        str8 = str8 + PLCConverter.ConvertStringFromAddress(data) + str12;
                                        ++num6;
                                        continue;
                                    }
                                    catch
                                    {
                                        string str13 = PLCConverter.Convert1WordStringFromData(data.Value);
                                        str7 = str7 + PLCConverter.ConvertStringFromAddress(data) + str13;
                                        ++num5;
                                        continue;
                                    }
                                }
                            }
                            string[] strArray = PLCConverter.Convert2WordsStringFrom4WordsData(data.Value);
                            str8 = str8 + PLCConverter.ConvertStringFromAddress(data) + strArray[0];
                            str8 += PLCConverter.ConvertStringFromAddress(data, 2);
                            str8 += strArray[1];
                            num6 += (byte)2;
                        }
                    }
                    string str14 = str6 + num5.ToString("X2") + num6.ToString("X2") + str7 + str8;
                    ushort length1 = (ushort)str14.Length;
                    buffer = Encoding.ASCII.GetBytes(str5 + length1.ToString("X4") + str14);
                    break;
            }
            lock (this._CommunicationLock)
            {
                this.m_Stream.Write(buffer, 0, buffer.Length);
                this.ReceiveMsg();
            }
        }

        private void SendMsg(
          IEnumerable<PLCSendingPacket> dataList,
          ref List<PLCReceivingPacket> readValArr)
        {
            byte[] buffer = (byte[])null;
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            List<byte> byteList1 = (List<byte>)null;
            List<byte> byteList2 = (List<byte>)null;
            byte num1 = 0;
            byte num2 = 0;
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList3 = new List<byte>()
          {
            (byte) 80,
            (byte) 0,
            this.NetworkNo,
            this.PCNo,
            byte.MaxValue,
            (byte) 3,
            (byte) 0
          };
                    List<byte> collection1 = new List<byte>(((IEnumerable<byte>)BitConverter.GetBytes(this.m_Setting.Timeout)).Take<byte>(2))
          {
            (byte) 3,
            (byte) 4,
            (byte) 0,
            (byte) 0
          };
                    List<byte> collection2 = new List<byte>();
                    List<byte> collection3 = new List<byte>();
                    foreach (PLCSendingPacket data in dataList)
                    {
                        if ((int)data.WordCount / 2 + (int)num2 > (int)byte.MaxValue)
                            throw new Exception("Too much send messages at once.");
                        byte num3 = (byte)((uint)data.WordCount / 2U);
                        if ((int)data.WordCount % 2 + (int)num1 > (int)byte.MaxValue)
                            throw new Exception("Too much send messages at once.");
                        byte num4 = (byte)((uint)data.WordCount % 2U);
                        for (int index = 0; index < (int)num3; ++index)
                            collection2.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, 2 * index));
                        if (num4 > (byte)0)
                            collection3.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromAddress(data, 2 * (int)num3));
                        num2 += num3;
                        num1 += num4;
                    }
                    collection1.Add(num1);
                    collection1.Add(num2);
                    collection1.AddRange((IEnumerable<byte>)collection3);
                    collection1.AddRange((IEnumerable<byte>)collection2);
                    ushort count = (ushort)collection1.Count;
                    byteList3.AddRange((IEnumerable<byte>)BitConverter.GetBytes(count));
                    byteList3.AddRange((IEnumerable<byte>)collection1);
                    buffer = byteList3.ToArray();
                    byteList3.Clear();
                    collection1.Clear();
                    collection2.Clear();
                    collection3.Clear();
                    byteList1 = (List<byte>)null;
                    byteList2 = (List<byte>)null;
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str1 = "5000" + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF00";
                    string str2 = this.m_Setting.Timeout.ToString("X4") + "04030000";
                    string empty3 = string.Empty;
                    string empty4 = string.Empty;
                    foreach (PLCSendingPacket data in dataList)
                    {
                        if ((int)data.WordCount / 2 + (int)num2 > (int)byte.MaxValue)
                            throw new Exception("Too much send messages at once.");
                        byte num5 = (byte)((uint)data.WordCount / 2U);
                        if ((int)data.WordCount % 2 + (int)num1 > (int)byte.MaxValue)
                            throw new Exception("Too much send messages at once.");
                        byte num6 = (byte)((uint)data.WordCount % 2U);
                        for (int index = 0; index < (int)num5; ++index)
                            empty4 += PLCConverter.ConvertStringFromAddress(data, 2 * index);
                        if (num6 > (byte)0)
                            empty3 += PLCConverter.ConvertStringFromAddress(data, 2 * (int)num5);
                        num2 += num5;
                        num1 += num6;
                    }
                    string str3 = str2 + num1.ToString("X2") + num2.ToString("X2") + empty3 + empty4;
                    ushort length = (ushort)str3.Length;
                    buffer = Encoding.ASCII.GetBytes(str1 + length.ToString("X4") + str3);
                    break;
            }
            lock (this._CommunicationLock)
            {
                this.m_Stream.Write(buffer, 0, buffer.Length);
                byte[] msg = this.ReceiveMsg((ushort)((uint)num2 * 2U + (uint)num1));
                IEnumerable<byte> source1 = ((IEnumerable<byte>)msg).Take<byte>((int)num1 * 2);
                IEnumerable<byte> source2 = ((IEnumerable<byte>)msg).Skip<byte>((int)num1 * 2);
                List<PLCReceivingPacket> plcReceivingPacketList = new List<PLCReceivingPacket>();
                foreach (PLCSendingPacket data in dataList)
                {
                    List<byte> byteList4 = new List<byte>();
                    int num7 = (int)data.WordCount / 2;
                    int num8 = (int)data.WordCount % 2;
                    List<byte> byteList5;
                    switch (this.ProtocolFormat)
                    {
                        case EPLCProtocolFormat.Binary:
                            if (num7 > 0)
                            {
                                byteList4.AddRange(source2.Take<byte>(num7 * 4));
                                source2 = source2.Skip<byte>(num7 * 4);
                            }
                            if (num8 > 0)
                            {
                                byteList4.AddRange(source1.Take<byte>(num8 * 2));
                                source1 = source1.Skip<byte>(num8 * 2);
                            }
                            if (byteList4.Count > 0)
                            {
                                plcReceivingPacketList.Add(new PLCReceivingPacket(byteList4.ToArray(), data.DeviceCode, data.Address));
                                byteList4.Clear();
                            }
                            byteList5 = (List<byte>)null;
                            continue;
                        case EPLCProtocolFormat.ASCII:
                            if (num7 > 0)
                            {
                                for (int index = 0; index < num7; ++index)
                                {
                                    IEnumerable<byte> source3 = source2.Skip<byte>(index * 4).Take<byte>(4);
                                    byteList4.AddRange(source3.Reverse<byte>());
                                }
                                source2 = source2.Skip<byte>(num7 * 4);
                            }
                            if (num8 > 0)
                            {
                                for (int index = 0; index < num8; ++index)
                                {
                                    IEnumerable<byte> source4 = source1.Skip<byte>(index * 2).Take<byte>(2);
                                    byteList4.AddRange(source4.Reverse<byte>());
                                }
                                source1 = source1.Skip<byte>(num8 * 2);
                            }
                            if (byteList4.Count > 0)
                            {
                                plcReceivingPacketList.Add(new PLCReceivingPacket(byteList4.ToArray(), data.DeviceCode, data.Address));
                                byteList4.Clear();
                            }
                            byteList5 = (List<byte>)null;
                            continue;
                        default:
                            continue;
                    }
                }
                readValArr = plcReceivingPacketList;
            }
        }

        private byte[] ReceiveMsg(ushort wordCount = 0)
        {
            string empty1 = string.Empty;
            string empty2 = string.Empty;

            Stopwatch sw = new Stopwatch();
            sw.Restart();
            while (this.m_CurrentData == null)
            {
                _currentDataOnAutoReset.WaitOne(15);
                if (sw.ElapsedMilliseconds >= Timeout) throw new TimeoutException($"Timeout occurred while waiting for PLC data. Timeout: {Timeout}ms");
            }
            sw.Stop();
            if (this.m_CurrentData.Length == 0)
            {
                this.m_CurrentData = (byte[])null;
                throw new Exception("Session disconnected.");
            }
            try
            {
                switch (this.ProtocolFormat)
                {
                    case EPLCProtocolFormat.Binary:
                        byte[] numArray = new byte[7]
                        {
              (byte) 208,
              (byte) 0,
              this.NetworkNo,
              this.PCNo,
              byte.MaxValue,
              (byte) 3,
              (byte) 0
                        };
                        for (int index = 0; index < numArray.Length; ++index)
                        {
                            if ((int)numArray[index] != (int)this.m_CurrentData[index])
                                throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + BitConverter.ToString(this.m_CurrentData));
                        }
                        ushort uint16 = BitConverter.ToUInt16(((IEnumerable<byte>)this.m_CurrentData).Skip<byte>(numArray.Length).Take<byte>(2).ToArray<byte>(), 0);
                        if ((int)uint16 != this.m_CurrentData.Length - numArray.Length - 2 || (int)uint16 != (int)wordCount * 2 + 2)
                            throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + BitConverter.ToString(this.m_CurrentData));
                        if (BitConverter.ToUInt16(((IEnumerable<byte>)this.m_CurrentData).Skip<byte>(numArray.Length + 2).Take<byte>(2).ToArray<byte>(), 0) != (ushort)0)
                            throw new Exception("Received error message." + Environment.NewLine + "Error code : " + PLCConverter.ConvertValueToString(((IEnumerable<byte>)this.m_CurrentData).Take<byte>(numArray.Length + 4).ToArray<byte>()));
                        return ((IEnumerable<byte>)this.m_CurrentData).Skip<byte>(numArray.Length + 4).ToArray<byte>();
                    case EPLCProtocolFormat.ASCII:
                        string str1 = Encoding.ASCII.GetString(this.m_CurrentData);
                        byte num1 = this.NetworkNo;
                        string str2 = num1.ToString("X2");
                        num1 = this.PCNo;
                        string str3 = num1.ToString("X2");
                        string str4 = "D000" + str2 + str3 + "03FF00";
                        if (!str1.Contains(str4))
                            throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + str1);
                        int num2 = int.Parse(str1.Substring(str4.Length, 4), NumberStyles.HexNumber);
                        if (num2 != str1.Length - str4.Length - 4 || num2 != (int)wordCount * 4 + 4)
                            throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + str1);
                        if (str1.Substring(str4.Length + 4, 4) != "0000")
                            throw new Exception("Received error message." + Environment.NewLine + "Message : " + str1.Substring(str4.Length + 8));
                        return PLCConverter.ConvertHexStringToByteArray(str1.Substring(str4.Length + 8));
                    default:
                        throw new Exception();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_CurrentData = (byte[])null;
            }
        }
    }
}
