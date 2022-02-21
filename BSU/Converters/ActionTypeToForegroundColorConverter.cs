using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BSU.Core.Model;

namespace BSU.GUI.Converters;

public class ActionTypeToForegroundColorConverter : IValueConverter
{
    public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
    {
        var value = (ModActionEnum)objValue;
        return value switch
        {
            ModActionEnum.UnusableSteam => new SolidColorBrush(Colors.DimGray),
            _ => new SolidColorBrush(Colors.Black)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
