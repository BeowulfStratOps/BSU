using System;
using System.Globalization;
using System.Windows.Data;
using BSU.Core.Model;

namespace BSU.GUI.Converters
{
    public class CalculatedStateToTextConverter : IValueConverter
    {
        public object Convert(object objValue, Type targetType, object parameter, CultureInfo culture)
        {
            var value = (CalculatedRepositoryStateEnum)objValue;
            return value switch
            {
                CalculatedRepositoryStateEnum.NeedsSync => "Requires Syncing",
                CalculatedRepositoryStateEnum.Ready => "Ready",
                CalculatedRepositoryStateEnum.RequiresUserIntervention => "Requires Changes",
                CalculatedRepositoryStateEnum.Syncing => "Syncing...",
                CalculatedRepositoryStateEnum.Loading => "Loading...",
                CalculatedRepositoryStateEnum.ReadyPartial => "Ready (partial preset)",
                CalculatedRepositoryStateEnum.Error => "Failed to load",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
