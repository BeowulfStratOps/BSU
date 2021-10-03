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
                case ModActionEnum.Await:
                case ModActionEnum.AbortActiveAndUpdate:
                    return "Update";
                case ModActionEnum.Use:
                    return "Use";
                case ModActionEnum.Unusable:
                    throw new InvalidOperationException();
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
