using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.JobManager
{
    /// <summary>
    /// Schedules and does work in a multi-threaded main-thread independent manner
    /// </summary>
    internal class JobManager : IJobManager
    {
        private const int MAX_THREADS = 5;
        
        private readonly IActionQueue _actionQueue;
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<WorkItem> _jobsTodo = new List<WorkItem>();
        private readonly List<WorkItem> _allJobs = new List<WorkItem>();
        private bool _shutdown;
        private readonly List<Thread> _workerThreads = new List<Thread>();

        public JobManager(IActionQueue actionQueue)
        {
            _actionQueue = actionQueue;
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
            
            var workItem = new WorkItem(job);

            lock (_allJobs)
            {
                _allJobs.Add(workItem);                
            }
            lock (_jobsTodo)
            {
                _jobsTodo.Add(workItem);
            }
            _actionQueue.EnQueueAction(() => JobAdded?.Invoke(job));
        }

        /// <summary>
        /// Returns all jobs ever queued.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IJob> GetAllJobs()
        {
            lock (_allJobs)
            {
                return _allJobs.Select(w => w.Job).ToList();
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
                return _jobsTodo.Select(w => w.Job).ToList();
            }
        }

        private void DoWork()
        {
            Logger.Debug("Worker thread starting");
            while (!_shutdown)
            {
                WorkItem job;
                lock (_jobsTodo)
                {
                    job = _jobsTodo.OrderBy(j => -j.Job.GetPriority()).FirstOrDefault();
                }

                if (job == null)
                {
                    Thread.Sleep(100); // TODO: make it interrupt-able by new job, lengthen sleep
                    continue;
                }

                var moreToDo = job.Job.DoWork(_actionQueue);

                lock (_jobsTodo)
                {
                    if (moreToDo) continue;
                    if (_jobsTodo.Remove(job)) // TODO: shouldn't that be an error otherwise?
                    {
                        job.TaskCompletionSource.SetResult(null);
                        Logger.Debug("Removed job {0}: {1}", job.Job.GetUid(), job.Job.GetTitle());
                    }
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
                    // TODO: set tcs
                    job.Job.Abort();
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
        
        private class WorkItem
        {
            public readonly IJob Job;
            public readonly TaskCompletionSource<object> TaskCompletionSource = new TaskCompletionSource<object>();

            public WorkItem(IJob job)
            {
                Job = job;
            }
        }
    }
}
