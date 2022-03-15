using System;
using System.Globalization;
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
            ModActionEnum.UnusableSteam => Theme.GetBrush("ButtonDisabled"),
            _ => Theme.GetBrush("SelectionForeground")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
