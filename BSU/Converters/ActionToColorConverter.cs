using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
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
                    return new SolidColorBrush(Colors.DimGray);
                case ModActionEnum.Update:
                case ModActionEnum.ContinueUpdate:
                case ModActionEnum.AbortAndUpdate:
                case ModActionEnum.Await:
                    return new SolidColorBrush(Colors.Blue);
                case ModActionEnum.Use:
                    return new SolidColorBrush(Colors.Green);
                case ModActionEnum.AbortActiveAndUpdate:
                    return new SolidColorBrush(Colors.Red);
                case ModActionEnum.Unusable:
                    return new SolidColorBrush(Colors.Red);
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
