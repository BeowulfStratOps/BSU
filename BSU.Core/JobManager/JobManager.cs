using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace BSU.Core.JobManager
{
    // TO--nvm-do: replace with tasks. use TaskCreationOptions.LongRunning where applicable (heavy IO). But then how to limit threads to 5??
    // TODO: use normal threadpool for quick stuff / high prio stuff
    
    /// <summary>
    /// Schedules and does work in a multi-threaded main-thread independent manner
    /// </summary>
    internal class JobManager : IJobManager
    {
        private const int MAX_THREADS = 5;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<IJob> _jobsTodo = new List<IJob>();
        private readonly List<IJob> _allJobs = new List<IJob>();
        private bool _shutdown;
        private readonly List<Thread> _workerThreads = new List<Thread>();

        public JobManager()
        {
            for (int i = 0; i < MAX_THREADS; i++)
            {
                var thread = new Thread(DoWork);
                _workerThreads.Add(thread);
                thread.Start();
            }
        }

        public event Action<IJob> JobAdded;

        /// <summary>
        /// Queue a job. Starts execution immediately
        /// </summary>
        /// <param name="job"></param>
        /// <exception cref="InvalidOperationException">Thrown if the manager is shutting down.</exception>
        public void QueueJob(IJob job)
        {
            Logger.Debug("Queueing job {0}: {1}", job.GetUid(), job.GetTitle());

            if (_shutdown) throw new InvalidOperationException("JobManager is shutting down! Come back tomorrow.");

            lock (_allJobs)
            {
                _allJobs.Add(job);                
            }
            lock (_jobsTodo)
            {
                _jobsTodo.Add(job);
            }
            JobAdded?.Invoke(job);
        }

        /// <summary>
        /// Returns all jobs ever queued.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IJob> GetAllJobs()
        {
            lock (_allJobs)
            {
                return _allJobs.AsReadOnly();
            }
        }

        /// <summary>
        /// Return all jobs currently running or queued.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IJob> GetActiveJobs()
        {
            lock (_jobsTodo)
            {
                return _jobsTodo.AsReadOnly();   
            }
        }

        private void DoWork()
        {
            Logger.Debug("Worker thread starting");
            while (!_shutdown)
            {
                IJob job;
                lock (_jobsTodo)
                {
                    job = _jobsTodo.OrderBy(j => -j.GetPriority()).FirstOrDefault();
                }

                if (job == null)
                {
                    Thread.Sleep(100); // TODO: make it interrupt-able by new job, lengthen sleep
                    continue;
                }

                var moreToDo = job.DoWork();

                lock (_jobsTodo)
                {
                    if (moreToDo) continue;
                    if (_jobsTodo.Remove(job)) Logger.Debug("Removed job {0}: {1}", job.GetUid(), job.GetTitle());
                }
            }
            Logger.Debug("Worker thread ending");
        }

        /// <summary>
        /// Shutdown all threads.
        /// </summary>
        public void Shutdown(bool blocking)
        {
            Logger.Info("JobManager shutting down");
            _shutdown = true;
            lock (_jobsTodo)
            {
                foreach (var job in _jobsTodo)
                {
                    job.Abort();
                }
            }

            if (!blocking) return;
            
            while (_workerThreads.Any())
            {
                foreach (var thread in new List<Thread>(_workerThreads))
                {
                    thread.Join(500);
                    if (!thread.IsAlive) _workerThreads.Remove(thread);
                }
            }
        }
    }
}
