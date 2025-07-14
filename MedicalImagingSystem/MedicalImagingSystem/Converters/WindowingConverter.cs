using MedicalImagingSystem.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.Converters
{
    public class WindowingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //if (values[0] is BitmapSource image &&
            //    values[1] is double windowWidth &&
            //    values[2] is double windowCenter)
            //{
            //    // 调用窗宽窗位处理逻辑
            //    return new DicomService().ApplyWindowing(image, windowWidth, windowCenter);
            //}
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
