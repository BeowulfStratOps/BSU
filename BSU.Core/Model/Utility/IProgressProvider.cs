using System.ComponentModel;

namespace BSU.Core.Model.Utility
{
    public interface IProgressProvider : INotifyPropertyChanged
    {
        string Stage { get; }
        bool IsIndeterminate { get; }
        double Value { get; }
    }
}