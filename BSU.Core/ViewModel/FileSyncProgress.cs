using System;
using BSU.Core.Sync;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class FileSyncProgress : ObservableBase
    {
        private bool _isIndeterminate;
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

        private bool _active;
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

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            private set
            {
                if (Math.Abs(value - _progressValue) < 0.0001) return;
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        private string _stage;
        public string Stage
        {
            get => _stage;
            private set
            {
                if (_stage == value) return;
                _stage = value;
                OnPropertyChanged();
            }
        }

        private FileSyncStats _stats;
        public FileSyncStats Stats
        {
            get => _stats;
            private set
            {
                if (_stats == value) return;
                _stats = value;
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
                    ProgressValue = 0;
                    IsIndeterminate = true;
                    Stage = "Preparing";
                    Stats = null;
                    break;
                }
                case FileSyncState.Updating:
                {
                    Active = true;
                    IsIndeterminate = false;
                    ProgressValue = (stats.DownloadDone + stats.UpdateDone) / (double)(stats.DownloadTotal + stats.UpdateTotal);
                    Stage = null;
                    Stats = stats;
                    break;
                }
                case FileSyncState.None:
                {
                    Active = false;
                    ProgressValue = 0;
                    IsIndeterminate = false;
                    Stage = null;
                    Stats = null;
                    break;
                }
                case FileSyncState.Stopping:
                    Active = true;
                    ProgressValue = 0;
                    IsIndeterminate = true;
                    Stage = "Stopping";
                    Stats = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
