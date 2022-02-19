using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BSU.Core.ViewModel.Util;

namespace BSU.GUI.Converters
{
    public class ColorIndicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (ColorIndication)value;
            return color switch
            {
                ColorIndication.Normal => new SolidColorBrush(Colors.Black),
                ColorIndication.Warning => new SolidColorBrush(Colors.Orange),
                ColorIndication.Primary => new SolidColorBrush(Colors.Green),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
