using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.CoreInterface;

namespace BSU.Core
{
    internal class SyncManager
    {
        private readonly List<UpdateJob> _jobsTodo = new List<UpdateJob>();
        private readonly List<UpdateJob> _allJobs = new List<UpdateJob>();
        private Thread _scheduler;

        public void QueueJob(UpdateJob job)
        {
            _allJobs.Add(job);
            lock (_jobsTodo)
            {
                _jobsTodo.Add(job);
            }

            if (_scheduler != null) return;
            _scheduler = new Thread(Schedule);
            _scheduler.Start();
        }

        public IReadOnlyList<UpdateJob> GetAllJobs() => _allJobs.AsReadOnly();

        private IWorkUnit GetWork()
        {
            lock (_jobsTodo)
            {
                if (!_jobsTodo.Any()) return null;
                var job = _jobsTodo.First();
                var work = job.SyncState.GetWork();
                if (work != null) return work;
                _jobsTodo.Remove(job);
                return null;
            }
        }

        private void DoWork()
        {
            while (true)
            {
                var work = GetWork();
                if (work == null) break;
                work.DoWork();
            }
        }

        private void Schedule()
        {
            var threads = new List<Thread>();

            for (int i = 0; i < 5; i++)
            {
                var thread = new Thread(DoWork);
                threads.Add(thread);
                thread.Start();
            }

            while (threads.Any())
            {
                foreach (var thread in new List<Thread>(threads))
                {
                    thread.Join(500);
                    if (!thread.IsAlive) threads.Remove(thread);
                }
            }
        }
    }
}