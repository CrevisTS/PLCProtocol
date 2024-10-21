using System;
using System.Globalization;
using System.Windows.Data;

namespace PLCCommunication_v2.Mitsubishi.Controls
{
    internal class BoolToRadioButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag ? (object)!flag : (object)false;
        }

        public object ConvertBack(
          object value,
          Type targetType,
          object parameter,
          CultureInfo culture)
        {
            return value is bool flag ? (object)!flag : (object)false;
        }
    }
}
