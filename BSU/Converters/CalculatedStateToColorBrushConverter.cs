using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BSU.Core.Model;

namespace BSU.GUI.Converters;

[ValueConversion(typeof(ModActionEnum), typeof(Brush))]
public class CalculatedStateToColorBrushConverter : IValueConverter
{
    public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
    {
        var value = (CalculatedRepositoryStateEnum)objValue;
        var color = value switch
        {
            CalculatedRepositoryStateEnum.NeedsSync => Colors.Blue,
            CalculatedRepositoryStateEnum.Ready => Colors.Green,
            CalculatedRepositoryStateEnum.RequiresUserIntervention => Colors.Red,
            CalculatedRepositoryStateEnum.Syncing => Colors.DimGray,
            CalculatedRepositoryStateEnum.Loading => Colors.DimGray,
            CalculatedRepositoryStateEnum.ReadyPartial => Colors.Orange,
            CalculatedRepositoryStateEnum.Error => Colors.Red,
            _ => throw new ArgumentOutOfRangeException()
        };
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
