using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace BSU.GUI.Converters;

public class ButtonEnabledToCursorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Cursors.Hand : Cursors.Arrow;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
