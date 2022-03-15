using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BSU.GUI.Converters;

public class StripeColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var index = (int)value;
        var suffix = parameter is "Hover" ? "Hover" : "";

        return index switch
        {
            0 => Theme.GetBrush("ModListBackground0" + suffix),
            1 => Theme.GetBrush("ModListBackground1" + suffix),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
