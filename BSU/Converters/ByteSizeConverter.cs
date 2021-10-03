using System;
using System.Globalization;
using System.Windows.Data;

namespace BSU.GUI.Converters
{
    public class ByteSizeConverter : IValueConverter
    {
        public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
        {
            var value = (long)objValue;
            var gb = (decimal)value / (1024 * 1024 * 1024);
            return $"{gb:n2} GB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
