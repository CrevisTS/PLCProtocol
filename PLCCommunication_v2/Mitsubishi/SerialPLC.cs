using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;


namespace PLCCommunication_v2.Mitsubishi
{
    public class SerialPLC : IPLC, IDisposable
    {
        private const byte ENQ = 5;
        private const byte EOT = 4;
        private const byte STX = 2;
        private const byte ETX = 3;
        private const byte ACK = 6;
        private const byte NAK = 21;
        private const byte CR = 13;
        private const byte LF = 10;
        private const byte CL = 12;
        private readonly char[] END_OF_TRANSMISSION = new char[3]
        {
      '\u0004',
      '\r',
      '\n'
        };
        private readonly char[] CLEAR_ALL_MESSAGES = new char[3]
        {
      '\f',
      '\r',
      '\n'
        };
        private readonly string PREFIX_STRING = new string(new char[1]
        {
      '\u0005'
        });
        private readonly string POSTFIX_STRING = new string(new char[2]
        {
      '\r',
      '\n'
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

        public byte HostStationNo
        {
            get => this.m_Setting != null ? this.m_Setting.HostStationNo : (byte)0;
            set
            {
                if (this.m_Setting == null)
                    return;
                this.m_Setting.HostStationNo = value;
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
            this.HostStationNo = (byte)0;
            this.NetworkNo = (byte)0;
            this.PCNo = byte.MaxValue;
        }

        public SerialPLC(
          string portName,
          int baudRate,
          int dataBits = 8,
          Parity parity = Parity.None,
          StopBits stopBits = StopBits.One,
          Handshake handshake = Handshake.None,
          byte hostStationNo = 0,
          byte networkNo = 0,
          byte pcNo = 255)
        {
            this.m_Setting = new SerialSetting();
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
            this.Handshake = handshake;
            this.HostStationNo = hostStationNo;
            this.NetworkNo = networkNo;
            this.PCNo = pcNo;
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
            while ((num1 = this.m_StringBuffer.IndexOf(this.POSTFIX_STRING)) != -1)
            {
                int num2 = num1 + this.POSTFIX_STRING.Length > this.m_StringBuffer.Length ? this.m_StringBuffer.Length : num1 + this.POSTFIX_STRING.Length;
                lock (this._CommunicationLock)
                {
                    this.m_CurrentString = this.m_StringBuffer.Substring(0, num2);
                    this.m_StringBuffer = this.m_StringBuffer.Remove(0, num2);
                    if (this.m_CurrentString.SequenceEqual<char>((IEnumerable<char>)this.END_OF_TRANSMISSION))
                    {
                        this.m_CurrentString = string.Empty;
                        this.m_StringBuffer = string.Empty;
                        this.m_Serial.Dispose();
                        break;
                    }
                    if (this.m_CurrentString.SequenceEqual<char>((IEnumerable<char>)this.CLEAR_ALL_MESSAGES))
                    {
                        this.m_CurrentString = string.Empty;
                        this.m_StringBuffer = string.Empty;
                        this.ClearMessages();
                        break;
                    }
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

        public void Write(IEnumerable<PLCSendingPacket> dataArr)
        {
            if (!this.IsConnected.HasValue || !this.IsConnected.Value)
                throw new Exception("PLC disconnected.");
            if (dataArr.Any<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(data => data.IsRead)))
                throw new Exception("Wrong PLC massage type : Must use write type messages.");
            this.SendMsg(dataArr);
        }

        public void Read(PLCSendingPacket data, ref PLCReceivingPacket receiveValue)
        {
            if (!this.IsConnected.HasValue || !this.IsConnected.Value)
                throw new Exception("PLC disconnected.");
            if (!data.IsRead)
                throw new Exception("Wrong PLC massage type : Must use read type message.");
            this.SendMsg(data, ref receiveValue);
        }

        public void Read(
          IEnumerable<PLCSendingPacket> dataArr,
          ref List<PLCReceivingPacket> receiveValueList)
        {
            if (!this.IsConnected.HasValue || !this.IsConnected.Value)
                throw new Exception("PLC disconnected.");
            if (dataArr.Any<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(data => !data.IsRead)))
                throw new Exception("Wrong PLC massage type : Must use read type messages.");
            this.SendMsg(dataArr, ref receiveValueList);
        }

        private void SendMsg(PLCSendingPacket data)
        {
            string empty1 = string.Empty;
            Type type = data.Value.GetType();
            string str1 = "F8" + this.HostStationNo.ToString("X2") + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF0000";
            string str2 = "1401";
            string str3;
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
                    ushort length = (ushort)str5.Length;
                    str3 = str4 + length.ToString("X4") + str5;
                }
            }
            else
            {
                string str6 = str2 + "0000" + PLCConverter.ConvertStringFromAddress(data);
                string empty2 = string.Empty;
                string str7 = !(data.Value is IEnumerable vals) || type == typeof(string) || data.Value is IEnumerable<char> || data.Value is IEnumerable<byte> ? PLCConverter.ConvertMultiWordsStringFromData(data.Value) : PLCConverter.ConvertMultiWordsStringFromDataList(vals);
                ushort num = (ushort)(str7.Length / 4);
                str3 = str6 + num.ToString("X4") + str7;
            }
            string checkSum = this.CalculateCheckSum(str1 + str3);
            string text = this.PREFIX_STRING + str1 + str3 + checkSum + this.POSTFIX_STRING;
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
            string empty = string.Empty;
            string str1 = "F8" + this.HostStationNo.ToString("X2") + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF0000";
            string str2 = "04010000" + PLCConverter.ConvertStringFromAddress(data) + data.WordCount.ToString("X4");
            string checkSum = this.CalculateCheckSum(str1 + str2);
            string text = this.PREFIX_STRING + str1 + str2 + checkSum + this.POSTFIX_STRING;
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
            receiveData = new PLCReceivingPacket(receivedData, data.DeviceCode, data.Address);
        }

        private void SendMsg(IEnumerable<PLCSendingPacket> dataList)
        {
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            string empty3 = string.Empty;
            IEnumerable<PLCSendingPacket> source1 = dataList.Where<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(item => item.Value is IEnumerable<bool> || item.Value is bool));
            IEnumerable<PLCSendingPacket> source2 = dataList.Where<PLCSendingPacket>((Func<PLCSendingPacket, bool>)(item => !(item.Value is IEnumerable<bool>) && !(item.Value is bool)));
            if (source1.Count<PLCSendingPacket>() > 0)
            {
                byte num1 = 0;
                string str1 = "F8" + this.HostStationNo.ToString("X2") + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF0000";
                string str2 = "14020001";
                string str3 = string.Empty;
                foreach (PLCSendingPacket data in source1)
                {
                    if (data.Value is IEnumerable<bool> source3)
                    {
                        int num2 = source3.Count<bool>();
                        for (int index = 0; index < num2; ++index)
                        {
                            str3 = str3 + PLCConverter.ConvertStringFromAddress(data, index) + (source3.ElementAt<bool>(index) ? "01" : "00");
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
                string checkSum = this.CalculateCheckSum(str1 + str4);
                string text = this.PREFIX_STRING + str1 + str4 + checkSum + this.POSTFIX_STRING;
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
            if (source2.Count<PLCSendingPacket>() <= 0)
                return;
            byte num3 = 0;
            byte num4 = 0;
            string str5 = "F8" + this.HostStationNo.ToString("X2") + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF0000";
            string str6 = "14020000";
            string str7 = string.Empty;
            string str8 = string.Empty;
            foreach (PLCSendingPacket data in source2)
            {
                string empty4 = string.Empty;
                if (data.Value is IEnumerable val)
                {
                    int offset1 = 0;
                    switch (val)
                    {
                        case string _:
                        case IEnumerable<char> _:
                            string str9 = PLCConverter.ConvertMultiWordsStringFromData((object)val);
                            while (str9.Length >= 8)
                            {
                                str8 = str8 + PLCConverter.ConvertStringFromAddress(data, offset1 * 2) + str9.Substring(4, 4) + str9.Substring(0, 4);
                                str9 = str9.Substring(8);
                                ++num4;
                                ++offset1;
                            }
                            if (str9.Length > 0)
                            {
                                str7 = str7 + PLCConverter.ConvertStringFromAddress(data, offset1 * 2) + str9;
                                ++num3;
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
                                                str8 = str8 + PLCConverter.ConvertStringFromAddress(data, offset1) + str10;
                                                offset1 += 2;
                                                ++num4;
                                                continue;
                                            }
                                            catch
                                            {
                                                string str11 = PLCConverter.Convert1WordStringFromData(current);
                                                str7 = str7 + PLCConverter.ConvertStringFromAddress(data, offset1) + str11;
                                                ++offset1;
                                                ++num3;
                                                continue;
                                            }
                                        }
                                    }
                                    string[] strArray = PLCConverter.Convert2WordsStringFrom4WordsData(current);
                                    str8 += PLCConverter.ConvertStringFromAddress(data, offset1);
                                    str8 += strArray[0];
                                    int offset2 = offset1 + 2;
                                    str8 += PLCConverter.ConvertStringFromAddress(data, offset2);
                                    str8 += strArray[1];
                                    offset1 = offset2 + 2;
                                    num4 += (byte)2;
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
                                ++num4;
                                continue;
                            }
                            catch
                            {
                                string str13 = PLCConverter.Convert1WordStringFromData(data.Value);
                                str7 = str7 + PLCConverter.ConvertStringFromAddress(data) + str13;
                                ++num3;
                                continue;
                            }
                        }
                    }
                    string[] strArray = PLCConverter.Convert2WordsStringFrom4WordsData(data.Value);
                    str8 = str8 + PLCConverter.ConvertStringFromAddress(data) + strArray[0];
                    str8 += PLCConverter.ConvertStringFromAddress(data, 2);
                    str8 += strArray[1];
                    num4 += (byte)2;
                }
            }
            string str14 = str6 + num3.ToString("X2") + num4.ToString("X2") + str7 + str8;
            string checkSum1 = this.CalculateCheckSum(str5 + str14);
            string text1 = this.PREFIX_STRING + str5 + str14 + checkSum1 + this.POSTFIX_STRING;
            AutoResetEvent executeEvent1 = (AutoResetEvent)null;
            lock (this._CommunicationLock)
            {
                this.m_Serial.Write(text1);
                lock (this._SequenceLock)
                {
                    executeEvent1 = new AutoResetEvent(false);
                    this.m_SequenceQueue.Enqueue(executeEvent1);
                }
            }
            this.ReceiveMsg(executeEvent1);
        }

        private void SendMsg(
          IEnumerable<PLCSendingPacket> dataList,
          ref List<PLCReceivingPacket> readValArr)
        {
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            string empty3 = string.Empty;
            byte num1 = 0;
            byte num2 = 0;
            string str1 = "F8" + this.HostStationNo.ToString("X2") + this.NetworkNo.ToString("X2") + this.PCNo.ToString("X2") + "03FF0000";
            string str2 = "04030000";
            string empty4 = string.Empty;
            string empty5 = string.Empty;
            foreach (PLCSendingPacket data in dataList)
            {
                if ((int)data.WordCount / 2 + (int)num2 > (int)byte.MaxValue)
                    throw new Exception("Too much send messages at once.");
                byte num3 = (byte)((uint)data.WordCount / 2U);
                if ((int)data.WordCount % 2 + (int)num1 > (int)byte.MaxValue)
                    throw new Exception("Too much send messages at once.");
                byte num4 = (byte)((uint)data.WordCount % 2U);
                for (int index = 0; index < (int)num3; ++index)
                    empty5 += PLCConverter.ConvertStringFromAddress(data, 2 * index);
                if (num4 > (byte)0)
                    empty4 += PLCConverter.ConvertStringFromAddress(data, 2 * (int)num3);
                num2 += num3;
                num1 += num4;
            }
            string str3 = str2 + num1.ToString("X2") + num2.ToString("X2") + empty4 + empty5;
            string checkSum = this.CalculateCheckSum(str1 + str3);
            string text = this.PREFIX_STRING + str1 + str3 + checkSum + this.POSTFIX_STRING;
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
            byte[] msg = this.ReceiveMsg(executeEvent, (ushort)((uint)num2 * 2U + (uint)num1));
            IEnumerable<byte> source1 = ((IEnumerable<byte>)msg).Take<byte>((int)num1 * 2);
            IEnumerable<byte> source2 = ((IEnumerable<byte>)msg).Skip<byte>((int)num1 * 2);
            List<PLCReceivingPacket> plcReceivingPacketList = new List<PLCReceivingPacket>();
            foreach (PLCSendingPacket data in dataList)
            {
                List<byte> byteList = new List<byte>();
                int num5 = (int)data.WordCount / 2;
                int num6 = (int)data.WordCount % 2;
                if (num5 > 0)
                {
                    for (int index = 0; index < num5; ++index)
                    {
                        IEnumerable<byte> source3 = source2.Skip<byte>(index * 4).Take<byte>(4);
                        byteList.AddRange(source3.Reverse<byte>());
                    }
                    source2 = source2.Skip<byte>(num5 * 4);
                }
                if (num6 > 0)
                {
                    for (int index = 0; index < num6; ++index)
                    {
                        IEnumerable<byte> source4 = source1.Skip<byte>(index * 2).Take<byte>(2);
                        byteList.AddRange(source4.Reverse<byte>());
                    }
                    source1 = source1.Skip<byte>(num6 * 2);
                }
                if (byteList.Count > 0)
                {
                    plcReceivingPacketList.Add(new PLCReceivingPacket(byteList.ToArray(), data.DeviceCode, data.Address));
                    byteList.Clear();
                }
            }
            readValArr = plcReceivingPacketList;
        }

        private string CalculateCheckSum(string msg)
        {
            int num = 0;
            foreach (char ch in msg)
                num += (int)ch;
            string str = num.ToString("X2");
            return str.Substring(str.Length - 2, 2);
        }

        private byte[] ReceiveMsg(AutoResetEvent executeEvent, ushort wordCount = 0)
        {
            string source = string.Empty;
            string empty = string.Empty;
            while (!executeEvent.WaitOne(100))
            {
                if (this.m_TerminateEvent.WaitOne(1))
                    throw new Exception("Session disconnected.");
            }
            lock (this._CommunicationLock)
            {
                source = this.m_CurrentString;
                this.m_CurrentString = string.Empty;
            }
            char ch;
            string hexString;
            if (wordCount == (ushort)0)
            {
                ch = source.First<char>();
                if (ch.Equals('\u0006'))
                {
                    string[] strArray = new string[6]
                    {
            this.m_Serial.Encoding.GetString(new byte[1]
            {
              (byte) 6
            }),
            "F8",
            null,
            null,
            null,
            null
                    };
                    byte num = this.HostStationNo;
                    strArray[2] = num.ToString("X2");
                    num = this.NetworkNo;
                    strArray[3] = num.ToString("X2");
                    num = this.PCNo;
                    strArray[4] = num.ToString("X2");
                    strArray[5] = "03FF0000";
                    string str = string.Concat(strArray);
                    if (!source.Contains(str))
                        throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + source);
                    hexString = "0000";
                    goto label_25;
                }
            }
            ch = source.First<char>();
            if (ch.Equals('\u0002'))
            {
                string[] strArray = new string[6]
                {
          this.m_Serial.Encoding.GetString(new byte[1]
          {
            (byte) 2
          }),
          "F8",
          null,
          null,
          null,
          null
                };
                byte num1 = this.HostStationNo;
                strArray[2] = num1.ToString("X2");
                num1 = this.NetworkNo;
                strArray[3] = num1.ToString("X2");
                num1 = this.PCNo;
                strArray[4] = num1.ToString("X2");
                strArray[5] = "03FF0000";
                string str = string.Concat(strArray);
                if (!source.Contains(str))
                    throw new Exception("Received wrong message. (Different header.)" + Environment.NewLine + "Message : " + source);
                string msg = source.Substring(1, source.Length - (3 + this.POSTFIX_STRING.Length));
                if (!source.Substring(source.Length - (2 + this.POSTFIX_STRING.Length), 2).Equals(this.CalculateCheckSum(msg)))
                    throw new Exception("Different checksum. (Maybe corrupted.)" + Environment.NewLine + "Message : " + source);
                int num2 = source.IndexOf('\u0003');
                if (num2 == -1)
                    throw new Exception("Received wrong message. (not include ETX in message.)" + Environment.NewLine + "Message : " + source);
                hexString = source.Substring(str.Length, source.Length - str.Length - (source.Length - num2));
            }
            else
            {
                ch = source.First<char>();
                if (ch.Equals('\u0015'))
                    throw new Exception("Received error message." + Environment.NewLine + "Error code : " + source.Substring(source.Length - (4 + this.POSTFIX_STRING.Length), 4));
                throw new Exception("Invalid message format." + Environment.NewLine + "Message : " + source);
            }
            label_25:
            return PLCConverter.ConvertHexStringToByteArray(hexString);
        }
    }
}
