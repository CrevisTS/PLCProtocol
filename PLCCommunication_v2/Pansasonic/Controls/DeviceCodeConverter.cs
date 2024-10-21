using System;
using System.Globalization;
using System.Windows.Data;

namespace PLCCommunication_v2.Panasonic.Controls
{
    internal class DeviceCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case EBinaryDeviceCode ebinaryDeviceCode:
                    return (object)(EPLCDeviceCode)ebinaryDeviceCode;
                case EContactReadableDeviceCode readableDeviceCode:
                    return (object)(EPLCDeviceCode)readableDeviceCode;
                case EContactWritableDeviceCode writableDeviceCode:
                    return (object)(EPLCDeviceCode)writableDeviceCode;
                case EDataDeviceCode edataDeviceCode:
                    return (object)(EPLCDeviceCode)edataDeviceCode;
                case EIndexRegisterDeviceCode registerDeviceCode:
                    return (object)(EPLCDeviceCode)registerDeviceCode;
                case EPLCDeviceCode eplcDeviceCode:
                    return (object)eplcDeviceCode;
                default:
                    return (object)null;
            }
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
