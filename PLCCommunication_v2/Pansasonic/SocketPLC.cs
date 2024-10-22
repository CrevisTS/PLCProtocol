using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;

namespace PLCCommunication_v2.Panasonic
{
    public class SocketPLC : IPLC, IDisposable
    {
        private readonly string CR = Encoding.ASCII.GetString(new byte[1]
        {
      (byte) 13
        });
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

        public byte UnitNo
        {
            get => this.m_Setting != null ? this.m_Setting.UnitNo : (byte)0;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.UnitNo = value;
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
            this.UnitNo = (byte)1;
            this.Timeout = 1000U;
        }

        public SocketPLC(
          string ipAddress,
          int portNum,
          EPLCProtocolFormat protocolFormat = EPLCProtocolFormat.Binary,
          byte unitNo = 1,
          uint timeout = 4000)
        {
            this.m_Setting = new SocketSetting();
            this.IP = ipAddress;
            this.PortNumber = portNum;
            this.ProtocolFormat = protocolFormat;
            this.UnitNo = unitNo;
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
            if (!File.Exists(this._DefaultPath + "\\Panasonic_Socket.xml"))
                this.Save();
            using (StreamReader streamReader = new StreamReader(this._DefaultPath + "\\Panasonic_Socket.xml"))
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
            using (StreamWriter streamWriter = new StreamWriter(this._DefaultPath + "\\Panasonic_Socket.xml"))
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

        public void Read(PLCSendingPacket data, ref PLCReceivingPacket receiveValue)
        {
            if (!this.IsConnected)
                throw new Exception("PLC not opened.");
            if (!data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use read type message.");
            this.SendMsg(data, ref receiveValue);
        }

        private void SendMsg(PLCSendingPacket data)
        {
            byte[] buffer = (byte[])null;
            Type type = data.Value.GetType();
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList = new List<byte>()
          {
            (byte) 128
          };
                    if (data.IsContact)
                    {
                        byteList.Add((byte)82);
                        byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromContactAddress(data));
                        byteList.Add(PLCConverter.Convert1BitByteFromBooleanData(data.Value is bool flag && flag));
                    }
                    else
                    {
                        byteList.Add((byte)80);
                        byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromDataAddress(data, PLCConverter.CalcWordCount(data.Value)));
                        if (data.Value is IEnumerable<bool> boolArr)
                            byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertNWordsByteArrayFromBooleanArrayData(boolArr));
                        else if (data.Value is IEnumerable vals && !(type == typeof(string)) && !(data.Value is IEnumerable<char>) && !(data.Value is IEnumerable<byte>))
                            byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertMultiWordsByteArrayFromDataList(vals));
                        else
                            byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertMultiWordsByteArrayFromData(data.Value));
                    }
                    buffer = byteList.ToArray();
                    byteList.Clear();
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str1 = "%" + this.UnitNo.ToString("00") + "#W";
                    string empty1 = string.Empty;
                    if (data.DeviceCode < EPLCDeviceCode.T)
                        throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
                    string str2;
                    string str3;
                    if (data.DeviceCode <= EPLCDeviceCode.C_L)
                    {
                        str2 = str1 + "C";
                        if (type == typeof(bool))
                        {
                            str3 = empty1 + "S" + PLCConverter.ConvertStringFromAddress(data) + PLCConverter.Convert1BitStringFromBooleanData(data.Value is bool flag && flag);
                        }
                        else
                        {
                            string str4 = empty1 + "C" + PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                            str3 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? str4 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : str4 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : str4 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                        }
                    }
                    else if (data.DeviceCode <= EPLCDeviceCode.F)
                    {
                        str2 = str1 + "D";
                        string str5 = empty1 + PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                        str3 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? str5 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : str5 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : str5 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                    }
                    else
                    {
                        str2 = str1 + "D";
                        string str6 = empty1 + PLCConverter.ConvertStringFromAddress(data);
                        string empty2 = string.Empty;
                        string str7 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? empty2 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : empty2 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : empty2 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                        if (data.DeviceCode <= EPLCDeviceCode.IY)
                        {
                            string str8 = str7.Length < 4 ? ("0000" + str7).Substring(str7.Length, 4) : str7.Substring(0, 4);
                            str3 = str6 + str8;
                        }
                        else
                        {
                            string str9 = str7.Length < 8 ? ("00000000" + str7).Substring(str7.Length, 8) : str7.Substring(0, 8);
                            str3 = str6 + str9;
                        }
                    }
                    buffer = Encoding.ASCII.GetBytes(str2 + str3 + PLCConverter.EncodeBCC(str2 + str3) + this.CR);
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
            switch (this.ProtocolFormat)
            {
                case EPLCProtocolFormat.Binary:
                    List<byte> byteList = new List<byte>()
          {
            (byte) 128
          };
                    if (data.IsContact)
                    {
                        byteList.Add((byte)83);
                        byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromContactAddress(data));
                        byteList.Add((byte)0);
                    }
                    else
                    {
                        byteList.Add((byte)81);
                        byteList.AddRange((IEnumerable<byte>)PLCConverter.ConvertByteArrayFromDataAddress(data, (int)data.WordCount));
                    }
                    buffer = byteList.ToArray();
                    byteList.Clear();
                    break;
                case EPLCProtocolFormat.ASCII:
                    string str1 = "%" + this.UnitNo.ToString("00") + "#R";
                    string empty = string.Empty;
                    if (data.DeviceCode < EPLCDeviceCode.T)
                        throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
                    string str2;
                    string str3;
                    if (data.DeviceCode <= EPLCDeviceCode.C_L)
                    {
                        str2 = str1 + "C";
                        str3 = empty + "C" + PLCConverter.ConvertStringFromAddress(data, (int)data.WordCount);
                    }
                    else if (data.DeviceCode <= EPLCDeviceCode.F)
                    {
                        str2 = str1 + "D";
                        str3 = empty + PLCConverter.ConvertStringFromAddress(data, (int)data.WordCount);
                    }
                    else
                    {
                        str2 = str1 + "D";
                        str3 = empty + PLCConverter.ConvertStringFromAddress(data);
                    }
                    buffer = Encoding.ASCII.GetBytes(str2 + str3 + PLCConverter.EncodeBCC(str2 + str3) + this.CR);
                    break;
            }
            lock (this._CommunicationLock)
            {
                this.m_Stream.Write(buffer, 0, buffer.Length);
                byte[] msg = this.ReceiveMsg(data.WordCount);
                receiveData = new PLCReceivingPacket(msg, data.DeviceCode, data.ContactAddress);
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
                        if (this.m_CurrentData.Length < 3)
                            throw new Exception("Received wrong message. (Wrong message type.)" + Environment.NewLine + "Message : " + BitConverter.ToString(this.m_CurrentData));
                        byte[] msg = this.m_CurrentData[2] == byte.MaxValue ? ((IEnumerable<byte>)this.m_CurrentData).Skip<byte>(3).ToArray<byte>() : throw new Exception("Received error message." + Environment.NewLine + "Error code : " + this.m_CurrentData[2].ToString("X2"));
                        if ((int)wordCount != msg.Length / 2)
                            throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + BitConverter.ToString(this.m_CurrentData));
                        return msg;
                    case EPLCProtocolFormat.ASCII:
                        string str = Encoding.ASCII.GetString(this.m_CurrentData);
                        if (str.Contains("!"))
                            throw new Exception("Received error message." + Environment.NewLine + "Message : " + str.Substring(4, 2));
                        if (!PLCConverter.DecodeBCC(str.Substring(0, str.Length - 3), str.Substring(str.Length - 3, 2)))
                            throw new Exception("Received wrong message. (Different BCC.)" + Environment.NewLine + "Message : " + str);
                        if (str.Contains("%FF") || str.Contains("<FF"))
                            return new byte[0];
                        if (str.Contains("$R"))
                        {
                            byte[] byteArray = PLCConverter.ConvertHexStringToByteArray(str.Substring(6, str.Length - 3));
                            if ((int)wordCount != byteArray.Length / 2)
                                throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + str);
                            return byteArray;
                        }
                        if (str.Contains("$W"))
                            return new byte[0];
                        throw new Exception("Received wrong message. (Unsupported format.)" + Environment.NewLine + "Message : " + str);
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