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
        return value switch
        {
            CalculatedRepositoryStateEnum.NeedsSync => Theme.GetBrush("IndicatorSync"),
            CalculatedRepositoryStateEnum.Ready => Theme.GetBrush("IndicatorGood"),
            CalculatedRepositoryStateEnum.RequiresUserIntervention => Theme.GetBrush("IndicatorError"),
            CalculatedRepositoryStateEnum.Syncing => Theme.GetBrush("IndicatorLoading"),
            CalculatedRepositoryStateEnum.Loading => Theme.GetBrush("IndicatorLoading"),
            CalculatedRepositoryStateEnum.ReadyPartial => Theme.GetBrush("IndicatorWarning"),
            CalculatedRepositoryStateEnum.Error => Theme.GetBrush("IndicatorError"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
