using System;
using System.Globalization;
using System.Windows.Data;

namespace PLCCommunication_v2.Panasonic.Controls
{
    internal class BoolToGreenRedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool flag))
                return (object)"Black";
            return flag ? (object)"LimeGreen" : (object)"Red";
        }

        public object ConvertBack(
          object value,
          Type targetType,
          object parameter,
          CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}