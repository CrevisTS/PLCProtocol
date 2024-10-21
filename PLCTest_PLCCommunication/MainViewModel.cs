using System;
using System.Windows;
using System.Windows.Input;
using CvsService.PLC.Mitsubishi.Enums;
using CvsService.PLC.Mitsubishi.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace PLCTest_PLCCommunication_v2
{
    public class MainViewModel : BindableBase
    {
        public IMitsubishiPLC PLC { get; }


        private string _iPAddress = "192.168.100.100";
        public string IPAddress { get => _iPAddress; set => SetProperty(ref _iPAddress, value); }

        private int _port = 8000;
        public int Port { get => _port; set => SetProperty(ref _port, value); }

        private EPLCFormat _format;
        public EPLCFormat Format { get => _format; set => SetProperty(ref _format, value); }

        private EPLCCode _writeCode = EPLCCode.D;
        public EPLCCode WriteCode { get => _writeCode; set => SetProperty(ref _writeCode, value); }
        
        private int _writeAddress;
        public int WriteAddress { get => _writeAddress; set => SetProperty(ref _writeAddress, value); }
        
        private string _writeValue;
        public string WriteValue { get => _writeValue; set => SetProperty(ref _writeValue, value); }

        private EPLCCode _readCode = EPLCCode.D;
        public EPLCCode ReadCode { get => _readCode; set => SetProperty(ref _readCode, value); }

        private int _readAddress;
        public int ReadAddress { get => _readAddress; set => SetProperty(ref _readAddress, value); }

        private ushort _readWC;
        public ushort ReadWordCount { get => _readWC; set => SetProperty(ref _readWC, value); }

        private string _readValue;
        public string ReadValue { get => _readValue; set => SetProperty(ref _readValue, value); }

        public ICommand BtnWriteClickCommand => new DelegateCommand(() => TryCatchAction(OnBtnWriteClick));
        public ICommand BtnReadClickCommand => new DelegateCommand(() => TryCatchAction(OnBtnReadClick));
        public ICommand BtnConnectClickCommand => new DelegateCommand(() => TryCatchAction(OnBtnConnectClick));
        public ICommand ClosingCommand => new DelegateCommand(() =>
        {
            if (PLC.IsConnected)
            {
                try { PLC.DisConnect(); } catch { }
            }
        });

        public MainViewModel(IMitsubishiPLC plc)
        {
            PLC = plc;
        }

        private void OnBtnConnectClick()
        {
            if (PLC.IsConnected)
            {
                PLC.DisConnect();
            }
            else
            {
                PLC.Connect(IPAddress, Port, Format);
            }
        }

        private void OnBtnWriteClick()
        {
            PLC.Write(WriteCode, WriteAddress, byte.Parse(WriteValue));
        }

        private void OnBtnReadClick()
        {
            ReadValue = PLC.ReadInt16(ReadCode, ReadAddress).ToString();
        }

        private void TryCatchAction(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
