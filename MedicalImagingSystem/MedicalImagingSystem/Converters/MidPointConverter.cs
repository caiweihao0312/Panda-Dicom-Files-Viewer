using System;
using System.Globalization;
using System.Windows.Data;

namespace MedicalImagingSystem.Converters
{
    public class MidPointConverter : IMultiValueConverter, IValueConverter
    {
        // 用于多值绑定
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double x1 && values[1] is double x2)
            {
                return (x1 + x2) / 2.0;
            }
            return 0.0;
        }

        // 用于单值绑定+ConverterParameter
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v1 && parameter is double v2)
            {
                return (v1 + v2) / 2.0;
            }
            if (value is double v && parameter != null && double.TryParse(parameter.ToString(), out double v2p))
            {
                return (v + v2p) / 2.0;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}