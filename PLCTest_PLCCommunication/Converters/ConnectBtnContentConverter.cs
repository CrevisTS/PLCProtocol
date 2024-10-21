using System;
using System.Globalization;
using System.Windows.Data;

namespace PLCTest_PLCCommunication_v2.Converters
{
    public class ConnectBtnContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnect)
            {
                if (isConnect) return "DisConnect";
                else return "Connect";
            }
            return "Convert Fail";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
