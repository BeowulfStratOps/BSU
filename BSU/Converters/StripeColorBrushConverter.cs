using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BSU.GUI.Converters;

public class StripeColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var app = Application.Current;;
        var index = (int)value;
        return index switch
        {
            0 => (Brush)app.FindResource("BackgroundColor1Brush")!,
            1 => new SolidColorBrush(Colors.White),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
