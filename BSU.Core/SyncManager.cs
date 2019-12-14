using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Sync;
using BSU.CoreInterface;

namespace BSU.Core
{
    internal class SyncManager : ISyncManager
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

            if (_scheduler != null && _scheduler.IsAlive) return;
            _scheduler = new Thread(Schedule);
            _scheduler.Start();
        }

        public IReadOnlyList<UpdateJob> GetAllJobs() => _allJobs.AsReadOnly();
        public IReadOnlyList<UpdateJob> GetActiveJobs() => _allJobs.Where(j => !j.SyncState.IsDone()).ToList().AsReadOnly();

        private WorkUnit GetWork()
        {
            lock (_jobsTodo)
            {
                if (!_jobsTodo.Any()) return null;
                var job = _jobsTodo.First();
                WorkUnit work;
                try
                {
                    work = job.SyncState.GetWork();
                }
                catch (Exception e)
                {
                    job.SyncState.SetError(e);
                    _jobsTodo.Remove(job);
                    return null;
                }
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
                try
                {
                    work.DoWork();
                }
                catch (Exception e)
                {
                    work.SetError(e);
                }
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