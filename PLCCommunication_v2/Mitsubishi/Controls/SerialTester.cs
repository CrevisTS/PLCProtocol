using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;


namespace PLCCommunication_v2.Mitsubishi.Controls
{
    public class SerialTester : UserControl, INotifyPropertyChanged, IComponentConnector
    {
        private SerialPLC m_PLC;
        private Thread m_ConnectionCheckThread;
        private char m_SeparateChar;
        private int m_SelectedSeparateCharIndex;
        internal SerialTester uc;
        internal Grid conn_Grid;
        internal DataGrid write_dataGrid;
        internal DataGrid read_dataGrid;
        private bool _contentLoaded;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }

        public string PortName
        {
            get => this.m_PLC != null ? this.m_PLC.PortName : string.Empty;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.PortName = value;
            }
        }

        public int BaudRate
        {
            get => this.m_PLC != null ? this.m_PLC.BaudRate : 0;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.BaudRate = value;
            }
        }

        public int DataBits
        {
            get => this.m_PLC != null ? this.m_PLC.DataBits : 0;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.DataBits = value;
            }
        }

        public Parity Parity
        {
            get => this.m_PLC != null ? this.m_PLC.Parity : Parity.None;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.Parity = value;
            }
        }

        public StopBits StopBits
        {
            get => this.m_PLC != null ? this.m_PLC.StopBits : StopBits.None;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.StopBits = value;
            }
        }

        public Handshake Handshake
        {
            get => this.m_PLC != null ? this.m_PLC.Handshake : Handshake.None;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.Handshake = value;
            }
        }

        public byte HostStationNo
        {
            get => this.m_PLC != null ? this.m_PLC.HostStationNo : (byte)0;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.HostStationNo = value;
            }
        }

        public byte NetworkNo
        {
            get => this.m_PLC != null ? this.m_PLC.NetworkNo : (byte)0;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.NetworkNo = value;
            }
        }

        public byte PCNo
        {
            get => this.m_PLC != null ? this.m_PLC.PCNo : byte.MaxValue;
            set
            {
                if (this.m_PLC == null)
                    return;
                this.m_PLC.PCNo = value;
            }
        }

        public bool? IsConnected => this.m_PLC != null ? this.m_PLC.IsConnected : new bool?();

        public int SelectedSeparateCharIndex
        {
            get => this.m_SelectedSeparateCharIndex;
            set
            {
                if (this.m_SelectedSeparateCharIndex == value)
                    return;
                this.m_SelectedSeparateCharIndex = value;
                switch (value)
                {
                    case 0:
                        this.m_SeparateChar = ' ';
                        break;
                    case 1:
                        this.m_SeparateChar = '-';
                        break;
                    case 2:
                        this.m_SeparateChar = ',';
                        break;
                    case 3:
                        this.m_SeparateChar = '_';
                        break;
                }
                foreach (ResultData result in (Collection<ResultData>)this.ResultList)
                    result.SeparateChar = this.m_SeparateChar;
            }
        }

        public ObservableCollection<string> PortList { get; set; }

        public ObservableCollection<SendCommand> WriteCommandList { get; set; }

        public ObservableCollection<SendCommand> ReadCommandList { get; set; }

        public ObservableCollection<ResultData> ResultList { get; set; }

        public SerialTester()
        {
            this.InitializeComponent();
            this.DataContext = (object)this;
            this.m_PLC = new SerialPLC();
            this.PortList = new ObservableCollection<string>((IEnumerable<string>)SerialPort.GetPortNames());
            this.WriteCommandList = new ObservableCollection<SendCommand>();
            this.ReadCommandList = new ObservableCollection<SendCommand>();
            this.ResultList = new ObservableCollection<ResultData>();
            this.m_SeparateChar = ' ';
        }

        private void Tester_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                this.OnClose();
            else
                this.OnLoad();
        }

        private void Connect_button_Click(object sender, RoutedEventArgs e)
        {
            this.Connect_button_Click();
        }

        private void Write_button_Click(object sender, RoutedEventArgs e) => this.Write_button_Click();

        private void Read_button_Click(object sender, RoutedEventArgs e) => this.Read_button_Click();

        private void NewWriteCommand_button_Click(object sender, RoutedEventArgs e)
        {
            this.NewWriteCommand_button_Click();
        }

        private void DeleteWriteCommand_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteWriteCommand_button_Click(this.write_dataGrid.SelectedIndex);
        }

        private void NewReadCommand_button_Click(object sender, RoutedEventArgs e)
        {
            this.NewReadCommand_button_Click();
        }

        private void DeleteReadCommand_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteReadCommand_button_Click(this.read_dataGrid.SelectedIndex);
        }

        private void UpdateProperties()
        {
            this.RaisePropertyChanged("PortName");
            this.RaisePropertyChanged("BaudRate");
            this.RaisePropertyChanged("DataBits");
            this.RaisePropertyChanged("Parity");
            this.RaisePropertyChanged("StopBits");
            this.RaisePropertyChanged("Handshake");
            this.RaisePropertyChanged("HostStationNo");
            this.RaisePropertyChanged("NetworkNo");
            this.RaisePropertyChanged("PCNo");
            this.RaisePropertyChanged("SelectedSeparateCharIndex");
        }

        private void OnLoad()
        {
            this.m_PLC.Load();
            this.UpdateProperties();
            if (this.m_ConnectionCheckThread != null && this.m_ConnectionCheckThread.IsAlive)
                return;
            this.m_ConnectionCheckThread = new Thread((ThreadStart)(() =>
            {
                while (true)
                {
                    this.RaisePropertyChanged("IsConnected");
                    Thread.Sleep(100);
                }
            }))
            {
                Name = "UI_ConnectionCheck"
            };
            this.m_ConnectionCheckThread.Start();
        }

        private void OnClose()
        {
            if (this.m_ConnectionCheckThread != null && this.m_ConnectionCheckThread.IsAlive)
            {
                this.m_ConnectionCheckThread.Abort();
                this.m_ConnectionCheckThread.Join(1000);
            }
            if (this.m_PLC == null)
                return;
            this.m_PLC.Save();
            this.m_PLC.Dispose();
        }

        private void Connect_button_Click()
        {
            if (this.IsConnected.HasValue)
            {
                try
                {
                    this.m_PLC.Disconnect();
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.Message);
                }
            }
            else
            {
                try
                {
                    this.m_PLC.Connect(this.PortName, this.BaudRate);
                }
                catch (Exception ex)
                {
                    int num = (int)MessageBox.Show(ex.Message);
                }
            }
        }

        private void Write_button_Click()
        {
            try
            {
                if (this.WriteCommandList.Count == 1)
                    this.m_PLC.Write(this.ConvertPLCData(this.WriteCommandList, 0));
                else if (this.WriteCommandList.Count > 0)
                {
                    List<PLCSendingPacket> dataArr = new List<PLCSendingPacket>();
                    for (int index = 0; index < this.WriteCommandList.Count; ++index)
                        dataArr.Add(this.ConvertPLCData(this.WriteCommandList, index));
                    this.m_PLC.Write((IEnumerable<PLCSendingPacket>)dataArr);
                }
                int num = (int)MessageBox.Show("Write Successfully.");
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message);
            }
        }

        private void Read_button_Click()
        {
            try
            {
                if (this.ReadCommandList.Count == 1)
                {
                    this.ResultList.Clear();
                    PLCSendingPacket data = this.ConvertPLCData(this.ReadCommandList, 0, true);
                    PLCReceivingPacket receiveValue = (PLCReceivingPacket)null;
                    this.m_PLC.Read(data, ref receiveValue);
                    this.ResultList.Add(new ResultData(receiveValue, this.m_SeparateChar));
                }
                else
                {
                    if (this.ReadCommandList.Count <= 0)
                        return;
                    this.ResultList.Clear();
                    List<PLCSendingPacket> dataArr = new List<PLCSendingPacket>();
                    List<PLCReceivingPacket> receiveValueList = (List<PLCReceivingPacket>)null;
                    for (int index = 0; index < this.ReadCommandList.Count; ++index)
                        dataArr.Add(this.ConvertPLCData(this.ReadCommandList, index, true));
                    this.m_PLC.Read((IEnumerable<PLCSendingPacket>)dataArr, ref receiveValueList);
                    for (int index = 0; index < receiveValueList.Count; ++index)
                        this.ResultList.Add(new ResultData(receiveValueList[index], this.m_SeparateChar));
                }
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message);
            }
        }

        private PLCSendingPacket ConvertPLCData(
          ObservableCollection<SendCommand> sendList,
          int index,
          bool isRead = false)
        {
            if (isRead)
                return new PLCSendingPacket(sendList[index].DeviceCode, sendList[index].DeviceNumber, isRead, sendList[index].WordCount);
            object obj = (object)null;
            string[] strArray = sendList[index].Value.Split(this.m_SeparateChar);
            switch (sendList[index].DataType)
            {
                case EParseDataType.Byte:
                    List<byte> byteList = new List<byte>();
                    foreach (string s in strArray)
                    {
                        byte result;
                        if (byte.TryParse(s, out result))
                            byteList.Add(result);
                    }
                    obj = (object)byteList;
                    break;
                case EParseDataType.Boolean:
                    List<bool> boolList = new List<bool>();
                    foreach (string str in strArray)
                    {
                        bool result;
                        if (bool.TryParse(str, out result))
                            boolList.Add(result);
                    }
                    obj = (object)boolList;
                    break;
                case EParseDataType.Short:
                    List<short> shortList = new List<short>();
                    foreach (string s in strArray)
                    {
                        short result;
                        if (short.TryParse(s, out result))
                            shortList.Add(result);
                    }
                    obj = (object)shortList;
                    break;
                case EParseDataType.Int:
                    List<int> intList = new List<int>();
                    foreach (string s in strArray)
                    {
                        int result;
                        if (int.TryParse(s, out result))
                            intList.Add(result);
                    }
                    obj = (object)intList;
                    break;
                case EParseDataType.Long:
                    List<long> longList = new List<long>();
                    foreach (string s in strArray)
                    {
                        long result;
                        if (long.TryParse(s, out result))
                            longList.Add(result);
                    }
                    obj = (object)longList;
                    break;
                case EParseDataType.Float:
                    List<float> floatList = new List<float>();
                    foreach (string s in strArray)
                    {
                        float result;
                        if (float.TryParse(s, out result))
                            floatList.Add(result);
                    }
                    obj = (object)floatList;
                    break;
                case EParseDataType.Double:
                    List<double> doubleList = new List<double>();
                    foreach (string s in strArray)
                    {
                        double result;
                        if (double.TryParse(s, out result))
                            doubleList.Add(result);
                    }
                    obj = (object)doubleList;
                    break;
                case EParseDataType.String:
                    obj = (object)sendList[index].Value;
                    break;
            }
            return new PLCSendingPacket(sendList[index].DeviceCode, sendList[index].DeviceNumber, obj);
        }

        private void NewWriteCommand_button_Click() => this.WriteCommandList.Add(new SendCommand());

        private void DeleteWriteCommand_button_Click(int selectedIndex)
        {
            if (selectedIndex == -1 || this.WriteCommandList.Count <= selectedIndex)
                return;
            this.WriteCommandList.RemoveAt(selectedIndex);
        }

        private void NewReadCommand_button_Click() => this.ReadCommandList.Add(new SendCommand());

        private void DeleteReadCommand_button_Click(int selectedIndex)
        {
            if (selectedIndex == -1 || this.ReadCommandList.Count <= selectedIndex)
                return;
            this.ReadCommandList.RemoveAt(selectedIndex);
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            if (this._contentLoaded)
                return;
            this._contentLoaded = true;
            Application.LoadComponent((object)this, new Uri("/PLCCommunication_v2;component/mitsubishi/controls/serialtester.xaml", UriKind.Relative));
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void IComponentConnector.Connect(int connectionId, object target)
        {
            switch (connectionId)
            {
                case 1:
                    this.uc = (SerialTester)target;
                    this.uc.IsVisibleChanged += new DependencyPropertyChangedEventHandler(this.Tester_IsVisibleChanged);
                    break;
                case 2:
                    this.conn_Grid = (Grid)target;
                    break;
                case 3:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.Connect_button_Click);
                    break;
                case 4:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.NewWriteCommand_button_Click);
                    break;
                case 5:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.DeleteWriteCommand_Button_Click);
                    break;
                case 6:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.Write_button_Click);
                    break;
                case 7:
                    this.write_dataGrid = (DataGrid)target;
                    break;
                case 8:
                    this.read_dataGrid = (DataGrid)target;
                    break;
                case 9:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.NewReadCommand_button_Click);
                    break;
                case 10:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.DeleteReadCommand_Button_Click);
                    break;
                case 11:
                    ((ButtonBase)target).Click += new RoutedEventHandler(this.Read_button_Click);
                    break;
                default:
                    this._contentLoaded = true;
                    break;
            }
        }
    }
}