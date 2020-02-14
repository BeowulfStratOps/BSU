using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.JobManager;

namespace BSU.Core.View
{
    public class Job : INotifyPropertyChanged
    {
        public string Title { get; }
        internal IJob BackingJob;
        public int Progress { get; private set; }

        internal Job(IJob backingJob)
        {
            BackingJob = backingJob;
            Title = backingJob.GetTitle();
            backingJob.Progress += () =>
            {
                var newValue = (int) (100 * backingJob.GetProgress());
                if (Progress == newValue) return;
                Progress = newValue;
                OnPropertyChanged(nameof(Progress));
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}