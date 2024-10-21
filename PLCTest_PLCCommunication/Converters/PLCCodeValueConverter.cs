using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using CvsService.PLC.Mitsubishi.Enums;

namespace PLCTest_PLCCommunication_v2.Converters
{
    public class PLCCodeValueConverter : IValueConverter
    {
        /// <summary>
        /// Enum타입인 Language를 StringArray로 변경하여 바인딩.
        /// </summary>
        public string[] PLCCodeStrings => GetStrings();

        // CustomControl사용 시 ConvertBack이 두번호출되는 문제 발생. 어떤 인덱스는 12 12 이렇게 두번 잘 떨어지는데 어떤 인덱스는 11 -1 이렇게 떨어지는 문제 있었음.
        private object beforeConvertVal = -1;
        private object beforeConvertBackVal = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // PLCCodeStrings에서 value.ToString()이 일치하는 인덱스를반환함
            if (value == null)
            {
                return beforeConvertVal;
            }
            int index = Array.IndexOf(PLCCodeStrings, value.ToString());

            beforeConvertVal = value;
            return index >= 0 ? index : beforeConvertVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is int index))
                return beforeConvertBackVal;

            if (index >= 0 && index < PLCCodeStrings.Length)
            {
                beforeConvertBackVal = PLCCodeStrings[index];
                return PLCCodeStrings[index];
            }

            return beforeConvertBackVal;
        }

        public static string GetString(EPLCCode value)
        {
            return value.ToString();
        }

        public static string[] GetStrings()
        {
            List<string> list = new List<string>();
            foreach (EPLCCode value in Enum.GetValues(typeof(EPLCCode)))
            {
                list.Add(GetString(value));
            }

            return list.ToArray();
        }
    }
}
