using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Sync;
using NLog;

namespace BSU.Core.JobManager
{
    internal class JobManager<TJobType> : IJobManager<TJobType> where TJobType : IJob
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<TJobType> _jobsTodo = new List<TJobType>();
        private readonly List<TJobType> _allJobs = new List<TJobType>();
        private bool _shutdown;
        private Thread _scheduler;

        public void QueueJob(TJobType job)
        {
            Logger.Debug("Queueing job {0}", job.GetUid());

            if (_shutdown) throw new InvalidOperationException("JobManager is shutting down! Come back tomorrow.");

            _allJobs.Add(job);
            lock (_jobsTodo)
            {
                _jobsTodo.Add(job);
            }

            if (_scheduler != null && _scheduler.IsAlive) return;
            Logger.Debug("Starting scheduler thread");
            _scheduler = new Thread(Schedule);
            _scheduler.Start();
        }

        public IEnumerable<TJobType> GetAllJobs() => _allJobs.AsReadOnly();
        public IEnumerable<TJobType> GetActiveJobs() => _allJobs.Where(j => !j.IsDone());

        private WorkUnit GetWork()
        {
            lock (_jobsTodo)
            {
                Logger.Trace("Getting work");
                if (!_jobsTodo.Any())
                {
                    Logger.Trace("No jobs");
                    return null;
                }

                var job = _jobsTodo.First();
                Logger.Trace("Checking job {0}", job.GetUid());
                WorkUnit work;
                try
                {
                    Logger.Trace("Getting work from job");
                    work = job.GetWork();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    job.SetError(e);
                    _jobsTodo.Remove(job);
                    job.CheckDone();
                    return null;
                }

                if (work != null)
                {
                    Logger.Trace("Got work: {0}", work);
                    return work;
                }

                Logger.Trace("No work. De-queueing job");
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
                    work.Work();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
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

            // TODO: thread count might get stuck on a lower number

            while (threads.Any())
            {
                foreach (var thread in new List<Thread>(threads))
                {
                    thread.Join(500);
                    if (!thread.IsAlive) threads.Remove(thread);
                }
            }

            Logger.Debug("Scheduler thread ending");
        }

        public void Shutdown()
        {
            if (_scheduler == null || !_scheduler.IsAlive) return;
            _shutdown = true;
            lock (_jobsTodo)
            {
                foreach (var job in _jobsTodo)
                {
                    job.Abort();
                }
            }

            _scheduler.Join();
        }
    }
}