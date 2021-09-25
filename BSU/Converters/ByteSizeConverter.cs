using System;
using System.Globalization;
using System.Windows.Data;

namespace BSU.GUI.Converters
{
    public class ByteSizeConverter : IValueConverter
    {
        static readonly string[] SizeSuffixes =
            { "B", "KB", "MB", "GB", "TB" };

        public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
        {
            // Based on https://stackoverflow.com/a/14488941
            var value = (long)objValue;
            if (value < 0) throw new NotImplementedException();

            var i = 0;
            var dValue = (decimal)value;
            while (Math.Round(dValue, 2) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return $"{dValue:n2} {SizeSuffixes[i]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
