using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;

namespace PLCCommunication_v2.Panasonic
{
    public class SerialPLC : IPLC, IDisposable
    {
        private readonly string CR = Encoding.ASCII.GetString(new byte[1]
        {
      (byte) 13
        });
        private readonly string _DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Check Box";
        private readonly object _CommunicationLock = new object();
        private readonly object _SequenceLock = new object();
        private readonly Queue<AutoResetEvent> m_SequenceQueue = new Queue<AutoResetEvent>();
        private readonly ManualResetEvent m_TerminateEvent = new ManualResetEvent(false);
        private SerialPort m_Serial;
        private SerialSetting m_Setting;
        private Thread m_ConnectionCheckThread;
        private string m_StringBuffer = string.Empty;
        private string m_CurrentString = string.Empty;

        public string PortName
        {
            get => this.m_Setting != null ? this.m_Setting.PortName : string.Empty;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.PortName = value;
            }
        }

        public int BaudRate
        {
            get => this.m_Setting != null ? this.m_Setting.BaudRate : -1;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.BaudRate = value;
            }
        }

        public int DataBits
        {
            get => this.m_Setting != null ? this.m_Setting.DataBits : -1;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.DataBits = value;
            }
        }

        public Parity Parity
        {
            get => this.m_Setting != null ? this.m_Setting.Parity : Parity.None;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.Parity = value;
            }
        }

        public StopBits StopBits
        {
            get => this.m_Setting != null ? this.m_Setting.StopBits : StopBits.One;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.StopBits = value;
            }
        }

        public Handshake Handshake
        {
            get => this.m_Setting != null ? this.m_Setting.Handshake : Handshake.None;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.Handshake = value;
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

        public bool? IsConnected
        {
            get
            {
                return this.m_Serial != null && this.m_Serial.IsOpen ? new bool?(this.m_Serial.Handshake == Handshake.None || this.m_Serial.DsrHolding) : new bool?();
            }
        }

        public SerialPLC()
        {
            this.m_Setting = new SerialSetting();
            this.PortName = "COM1";
            this.BaudRate = 9600;
            this.DataBits = 8;
            this.Parity = Parity.None;
            this.StopBits = StopBits.One;
            this.Handshake = Handshake.None;
        }

        public SerialPLC(
          string portName,
          int baudRate,
          int dataBits = 8,
          Parity parity = Parity.None,
          StopBits stopBits = StopBits.One,
          Handshake handshake = Handshake.None)
        {
            this.m_Setting = new SerialSetting();
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
        }

        public void Dispose()
        {
            this.Disconnect();
            if (this.m_Serial == null)
                return;
            this.m_Serial.Dispose();
        }

        public void Load()
        {
            if (!File.Exists(this._DefaultPath + "\\Mitsubishi_Serial.xml"))
                this.Save();
            using (StreamReader streamReader = new StreamReader(this._DefaultPath + "\\Mitsubishi_Serial.xml"))
            {
                if (!(new XmlSerializer(typeof(SerialSetting)).Deserialize((TextReader)streamReader) is SerialSetting serialSetting))
                    serialSetting = this.m_Setting;
                this.m_Setting = serialSetting;
            }
        }

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                this.Save(filePath);
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                if (!(new XmlSerializer(typeof(SerialSetting)).Deserialize((TextReader)streamReader) is SerialSetting serialSetting))
                    serialSetting = this.m_Setting;
                this.m_Setting = serialSetting;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(this._DefaultPath);
            using (StreamWriter streamWriter = new StreamWriter(this._DefaultPath + "\\Mitsubishi_Serial.xml"))
                new XmlSerializer(typeof(SerialSetting)).Serialize((TextWriter)streamWriter, (object)(this.m_Setting ?? new SerialSetting()));
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
                new XmlSerializer(typeof(SerialSetting)).Serialize((TextWriter)streamWriter, (object)(this.m_Setting ?? new SerialSetting()));
        }

        public void Connect()
        {
            if (this.IsConnected.HasValue)
                throw new Exception("Already Opened.");
            this.SerialConnect();
            if (this.m_ConnectionCheckThread != null && this.m_ConnectionCheckThread.IsAlive)
                return;
            this.m_ConnectionCheckThread = new Thread(new ThreadStart(this.OnCheckProcess));
            this.m_ConnectionCheckThread.Start();
        }

        public void Connect(string portName, int baudRate)
        {
            if (this.IsConnected.HasValue)
                throw new Exception("Already Opened.");
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Connect();
        }

        public void Connect(
          string portName,
          int baudRate,
          int dataBits,
          Parity parity,
          StopBits stopBits,
          Handshake handshake)
        {
            if (this.IsConnected.HasValue)
                throw new Exception("Already Opened.");
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
            this.Connect();
        }

        private void SerialConnect()
        {
            this.m_Serial = new SerialPort()
            {
                PortName = this.PortName,
                BaudRate = this.BaudRate,
                Parity = this.Parity,
                DataBits = this.DataBits,
                StopBits = this.StopBits,
                Handshake = this.Handshake
            };
            this.m_Serial.DataReceived += new SerialDataReceivedEventHandler(this.Serial_DataReceived);
            this.m_Serial.Open();
            this.m_Serial.DtrEnable = true;
            this.m_TerminateEvent.Reset();
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.m_StringBuffer += this.m_Serial.ReadExisting();
            int num1;
            while ((num1 = this.m_StringBuffer.IndexOf(this.CR)) != -1)
            {
                int num2 = num1 + this.CR.Length > this.m_StringBuffer.Length ? this.m_StringBuffer.Length : num1 + this.CR.Length;
                lock (this._CommunicationLock)
                {
                    this.m_CurrentString = this.m_StringBuffer.Substring(0, num2);
                    this.m_StringBuffer = this.m_StringBuffer.Remove(0, num2);
                }
                lock (this._SequenceLock)
                {
                    if (this.m_SequenceQueue.Count > 0)
                        this.m_SequenceQueue.Dequeue().Set();
                }
            }
        }

        private void OnCheckProcess()
        {
            try
            {
                int num = 0;
                while (true)
                {
                    while (!this.IsConnected.HasValue || !this.IsConnected.Value)
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
                            if (this.ReconnectCount == (ushort)0)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            if (num > (int)this.ReconnectCount)
                                throw new Exception("PLC reconnection failed : " + ex.Message + Environment.NewLine + "Please check serial cable or PLC power.");
                            ++num;
                            Thread.Sleep(100);
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

        public void Disconnect()
        {
            if (this.m_ConnectionCheckThread != null && this.m_ConnectionCheckThread.IsAlive)
            {
                this.m_ConnectionCheckThread.Abort();
                this.m_ConnectionCheckThread.Join(1000);
            }
            this.SerialDisconnect();
        }

        private void ClearMessages()
        {
            lock (this._SequenceLock)
            {
                if (this.m_SequenceQueue != null)
                    this.m_SequenceQueue.Clear();
            }
            if (this.m_Serial != null && this.m_Serial.IsOpen)
            {
                this.m_Serial.DiscardInBuffer();
                this.m_Serial.DiscardOutBuffer();
            }
            this.m_TerminateEvent.Set();
        }

        private void SerialDisconnect()
        {
            this.ClearMessages();
            if (this.m_Serial == null)
                return;
            this.m_Serial.DataReceived -= new SerialDataReceivedEventHandler(this.Serial_DataReceived);
            this.m_Serial.Close();
        }

        public void Refresh()
        {
            this.SerialDisconnect();
            this.SerialConnect();
        }

        public void Write(PLCSendingPacket data)
        {
            if (!this.IsConnected.HasValue || !this.IsConnected.Value)
                throw new Exception("PLC disconnected.");
            if (data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use write type message.");
            this.SendMsg(data);
        }

        public void Read(PLCSendingPacket data, ref PLCReceivingPacket receiveValue)
        {
            if (!this.IsConnected.HasValue || !this.IsConnected.Value)
                throw new Exception("PLC disconnected.");
            if (!data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use read type message.");
            this.SendMsg(data, ref receiveValue);
        }

        private void SendMsg(PLCSendingPacket data)
        {
            string empty1 = string.Empty;
            Type type = data.Value.GetType();
            string str1 = "%" + this.UnitNo.ToString("00") + "#W";
            string empty2 = string.Empty;
            if (data.DeviceCode < EPLCDeviceCode.T)
                throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            string str2;
            string str3;
            if (data.DeviceCode <= EPLCDeviceCode.C_L)
            {
                str2 = str1 + "C";
                if (type == typeof(bool))
                {
                    str3 = empty2 + "S" + PLCConverter.ConvertStringFromAddress(data) + PLCConverter.Convert1BitStringFromBooleanData(data.Value is bool flag && flag);
                }
                else
                {
                    string str4 = empty2 + "C" + PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                    str3 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? str4 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : str4 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : str4 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
                }
            }
            else if (data.DeviceCode <= EPLCDeviceCode.F)
            {
                str2 = str1 + "D";
                string str5 = empty2 + PLCConverter.ConvertStringFromAddress(data, PLCConverter.CalcWordCount(data.Value));
                str3 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? str5 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : str5 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : str5 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
            }
            else
            {
                str2 = str1 + "D";
                string str6 = empty2 + PLCConverter.ConvertStringFromAddress(data);
                string empty3 = string.Empty;
                string str7 = !(data.Value is IEnumerable<bool> boolArr) ? (!(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? empty3 + PLCConverter.ConvertMultiWordsStringFromData(data.Value) : empty3 + PLCConverter.ConvertMultiWordsStringFromDataList(vals)) : empty3 + PLCConverter.ConvertNWordsStringFromBooleanArrayData(boolArr);
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
            string text = str2 + str3 + PLCConverter.EncodeBCC(str2 + str3) + this.CR;
            AutoResetEvent executeEvent = (AutoResetEvent)null;
            lock (this._CommunicationLock)
            {
                this.m_Serial.Write(text);
                lock (this._SequenceLock)
                {
                    executeEvent = new AutoResetEvent(false);
                    this.m_SequenceQueue.Enqueue(executeEvent);
                }
            }
            this.ReceiveMsg(executeEvent);
        }

        private void SendMsg(PLCSendingPacket data, ref PLCReceivingPacket receiveData)
        {
            string empty1 = string.Empty;
            string str1 = "%" + this.UnitNo.ToString("00") + "#R";
            string empty2 = string.Empty;
            if (data.DeviceCode < EPLCDeviceCode.T)
                throw new Exception("Invalid device code - Code : " + data.DeviceCode.ToString());
            string str2;
            string str3;
            if (data.DeviceCode <= EPLCDeviceCode.C_L)
            {
                str2 = str1 + "C";
                str3 = empty2 + "C" + PLCConverter.ConvertStringFromAddress(data, (int)data.WordCount);
            }
            else if (data.DeviceCode <= EPLCDeviceCode.F)
            {
                str2 = str1 + "D";
                str3 = empty2 + PLCConverter.ConvertStringFromAddress(data, (int)data.WordCount);
            }
            else
            {
                str2 = str1 + "D";
                str3 = empty2 + PLCConverter.ConvertStringFromAddress(data);
            }
            string text = str2 + str3 + PLCConverter.EncodeBCC(str2 + str3) + this.CR;
            AutoResetEvent executeEvent = (AutoResetEvent)null;
            lock (this._CommunicationLock)
            {
                this.m_Serial.Write(text);
                lock (this._SequenceLock)
                {
                    executeEvent = new AutoResetEvent(false);
                    this.m_SequenceQueue.Enqueue(executeEvent);
                }
            }
            byte[] msg = this.ReceiveMsg(executeEvent, data.WordCount);
            byte[] receivedData = new byte[msg.Length];
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
            receiveData = new PLCReceivingPacket(receivedData, data.DeviceCode, data.ContactAddress);
        }

        private byte[] ReceiveMsg(AutoResetEvent executeEvent, ushort wordCount = 0)
        {
            string str = string.Empty;
            string empty = string.Empty;
            while (!executeEvent.WaitOne(100))
            {
                if (this.m_TerminateEvent.WaitOne(1))
                    throw new Exception("Session disconnected.");
            }
            lock (this._CommunicationLock)
            {
                str = this.m_CurrentString;
                this.m_CurrentString = string.Empty;
            }
            if (str.Contains("!"))
                throw new Exception("Received error message." + Environment.NewLine + "Message : " + str.Substring(4, 2));
            if (!PLCConverter.DecodeBCC(str.Substring(0, str.Length - 3), str.Substring(str.Length - 3, 2)))
                throw new Exception("Received wrong message. (Different BCC.)" + Environment.NewLine + "Message : " + str);
            if (str.Contains("%FF") || str.Contains("<FF"))
                return new byte[0];
            if (str.Contains("$R"))
            {
                if ((int)wordCount != str.Substring(6, str.Length - 3).Length / 4)
                    throw new Exception("Received wrong message. (Different message length.)" + Environment.NewLine + "Message : " + str);
                return PLCConverter.ConvertHexStringToByteArray(str.Substring(6, str.Length - 3));
            }
            if (str.Contains("$W"))
                return new byte[0];
            throw new Exception("Received wrong message. (Unsupported format.)" + Environment.NewLine + "Message : " + str);
        }
    }
}
