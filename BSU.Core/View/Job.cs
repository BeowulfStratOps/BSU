using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.JobManager;
using BSU.Core.Sync;

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
            if (!(backingJob is IJobProgress jobProgress)) return; // TODO: set to infinite thingy
            jobProgress.OnProgress += () =>
            {
                var newValue = (int) (100 * jobProgress.GetProgress());
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