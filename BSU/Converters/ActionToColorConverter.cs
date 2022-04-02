using System;
using System.Globalization;
using System.Windows.Data;
using BSU.Core.Model;

namespace BSU.GUI.Converters
{
    public class ActionToColorConverter : IValueConverter
    {
        public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
        {
            var value = (ModActionEnum)objValue;
            switch (value)
            {
                case ModActionEnum.Loading:
                    return Theme.GetBrush("IndicatorLoading");
                case ModActionEnum.Update:
                case ModActionEnum.ContinueUpdate:
                case ModActionEnum.AbortAndUpdate:
                case ModActionEnum.Await:
                    return Theme.GetBrush("IndicatorSync");
                case ModActionEnum.Use:
                    return Theme.GetBrush("IndicatorGood");
                case ModActionEnum.AbortActiveAndUpdate:
                case ModActionEnum.Unusable:
                case ModActionEnum.UnusableSteam:
                    return Theme.GetBrush("IndicatorError");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
