using System.ComponentModel;

namespace PLCCommunication_v2.Mitsubishi.Controls
{
    public class ResultData : INotifyPropertyChanged
    {
        private PLCReceivingPacket m_ReceiveData;
        private EParseDataType m_DataType;
        private char m_SeparateChar;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }

        public EPLCDeviceCode DeviceCode
        {
            get => this.m_ReceiveData != null ? this.m_ReceiveData.DeviceCode : EPLCDeviceCode.M;
        }

        public int DeviceNumber => this.m_ReceiveData != null ? this.m_ReceiveData.Address : -1;

        public string DeviceHexNumber
        {
            get => this.m_ReceiveData != null ? this.m_ReceiveData.Address.ToString("X") : "-1";
        }

        public char SeparateChar
        {
            get => this.m_SeparateChar;
            set
            {
                if ((int)this.m_SeparateChar == (int)value)
                    return;
                this.m_SeparateChar = value;
                if (this.m_ReceiveData == null)
                    return;
                this.ChangeResultText();
            }
        }

        public EParseDataType DataType
        {
            get => this.m_DataType;
            set
            {
                this.m_DataType = value;
                if (this.m_ReceiveData == null)
                    return;
                this.ChangeResultText();
            }
        }

        public string ResultText { get; private set; }

        public ResultData(PLCReceivingPacket receiveData, char separateChar)
        {
            this.m_ReceiveData = receiveData;
            this.SeparateChar = separateChar;
            this.DataType = EParseDataType.Byte;
        }

        private void ChangeResultText()
        {
            this.ResultText = string.Empty;
            switch (this.m_DataType)
            {
                case EParseDataType.Boolean:
                    foreach (bool boolean in this.m_ReceiveData.GetBooleanArray())
                        this.ResultText = this.ResultText + (boolean ? "True" : "False") + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.Short:
                    foreach (short int16 in this.m_ReceiveData.GetInt16Array())
                        this.ResultText = this.ResultText + int16.ToString() + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.Int:
                    foreach (int int32 in this.m_ReceiveData.GetInt32Array())
                        this.ResultText = this.ResultText + int32.ToString() + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.Long:
                    foreach (long int64 in this.m_ReceiveData.GetInt64Array())
                        this.ResultText = this.ResultText + int64.ToString() + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.Float:
                    foreach (float single in this.m_ReceiveData.GetSingleArray())
                        this.ResultText = this.ResultText + single.ToString() + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.Double:
                    foreach (double num in this.m_ReceiveData.GetDoubleArray())
                        this.ResultText = this.ResultText + num.ToString() + this.m_SeparateChar.ToString();
                    break;
                case EParseDataType.String:
                    this.ResultText = this.m_ReceiveData.GetASCIIString() + this.m_SeparateChar.ToString();
                    break;
                default:
                    foreach (byte num in this.m_ReceiveData.GetByteArray())
                        this.ResultText = this.ResultText + num.ToString("X2") + this.m_SeparateChar.ToString();
                    break;
            }
            if (this.ResultText.Length > 0)
                this.ResultText = this.ResultText.Remove(this.ResultText.Length - 1, 1);
            this.RaisePropertyChanged("ResultText");
        }
    }
}
