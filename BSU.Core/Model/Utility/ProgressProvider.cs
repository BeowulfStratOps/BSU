using BSU.Core.ViewModel.Util;

namespace BSU.Core.Model.Utility
{
    public class ProgressProvider : ObservableBase, IProgressProvider
    {
        private bool _isIndeterminate = true;
        private double _value;
        private string _stage;

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            internal set
            {
                if (_isIndeterminate == value) return;
                _isIndeterminate = value;
                OnPropertyChanged();
            }
        }

        public double Value
        {
            get => _value;
            internal set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public string Stage
        {
            get => _stage;
            internal set
            {
                if (_stage == value) return;
                _stage = value;
                OnPropertyChanged();
            }
        }
    }
}