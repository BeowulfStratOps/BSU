using System;
using System.Globalization;
using System.Windows.Data;

namespace BSU.GUI.Converters;

public class EnableToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Theme.GetBrush("ButtonNormal") : Theme.GetBrush("ButtonDisabled");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
