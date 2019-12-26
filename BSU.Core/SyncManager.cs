using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core
{
    internal class SyncManager : ISyncManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<UpdateJob> _jobsTodo = new List<UpdateJob>();
        private readonly List<UpdateJob> _allJobs = new List<UpdateJob>();
        private bool _shutdown;
        private Thread _scheduler;

        public void QueueJob(UpdateJob job)
        {
            Logger.Debug("Queueing job {0} -> {1}", job.StorageMod.GetUid(), job.RepositoryMod.GetUid());

            if (_shutdown) throw new InvalidOperationException("SyncManager is shutting down! Come back tomorrow.");

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

        public IReadOnlyList<UpdateJob> GetAllJobs() => _allJobs.AsReadOnly();
        public IReadOnlyList<UpdateJob> GetActiveJobs() => _allJobs.Where(j => !j.SyncState.IsDone()).ToList().AsReadOnly();

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
                Logger.Trace("Checking job {0} -> {1}", job.StorageMod.GetUid(), job.RepositoryMod.GetUid());
                WorkUnit work;
                try
                {
                    Logger.Trace("Getting work from job");
                    work = job.SyncState.GetWork();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    job.SyncState.SetError(e);
                    _jobsTodo.Remove(job);
                    job.SyncState.CheckDone();
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
                    job.SyncState.Abort();
                }
            }
            _scheduler.Join();
        }
    }
}
