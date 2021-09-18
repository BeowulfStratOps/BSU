using System;
using BSU.Core.Sync;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class FileSyncProgress : ObservableBase
    {
        private bool _isIndeterminate = false;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            private set
            {
                if (value == _isIndeterminate) return;
                _isIndeterminate = value;
                OnPropertyChanged();
            }
        }

        private bool _active = false;
        public bool Active
        {
            get => _active;
            private set
            {
                if (value == _active) return;
                _active = value;
                OnPropertyChanged();
            }
        }

        private double _value = 0;
        public double Value
        {
            get => _value;
            private set
            {
                if (Math.Abs(value - _value) < 0.0001) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        internal Progress<FileSyncStats> Progress { get; } = new();

        public FileSyncProgress()
        {
            Progress.ProgressChanged += ProgressOnProgressChanged;
        }

        private void ProgressOnProgressChanged(object sender, FileSyncStats stats)
        {
            switch (stats.State)
            {
                case FileSyncState.Waiting:
                {
                    Active = true;
                    Value = 0;
                    IsIndeterminate = true;
                    break;
                }
                case FileSyncState.Updating:
                {
                    Active = true;
                    IsIndeterminate = false;
                    Value = (stats.DownloadDone + stats.UpdateDone) / (double)(stats.DownloadTotal + stats.UpdateTotal);
                    break;
                }
                case FileSyncState.None:
                {
                    Active = false;
                    Value = 0;
                    IsIndeterminate = false;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
