using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BSU.Core.Model;

namespace BSU.GUI.Converters
{
    public class ActionToTextConverter : IValueConverter
    {
        public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
        {
            var value = (ModActionEnum)objValue;
            switch (value)
            {
                case ModActionEnum.Update:
                case ModActionEnum.ContinueUpdate:
                case ModActionEnum.AbortAndUpdate:
                case ModActionEnum.AbortActiveAndUpdate:
                    return "Update";
                case ModActionEnum.Await:
                    return "Updating";
                case ModActionEnum.Use:
                    return "Use";
                case ModActionEnum.Loading:
                    return "Loading";
                case ModActionEnum.Unusable:
                    return "Unusable";
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
