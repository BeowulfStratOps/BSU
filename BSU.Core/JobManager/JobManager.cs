using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.Sync;
using NLog;

namespace BSU.Core.JobManager
{
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
        private Thread _scheduler;
        private object _counterLock = new object();
        private int _threadsDone;

        public event Action<IJob> JobAdded, JobRemoved;

        /// <summary>
        /// Queue a job. Starts execution immediately
        /// </summary>
        /// <param name="job"></param>
        /// <exception cref="InvalidOperationException">Thrown if the manager is shutting down.</exception>
        public void QueueJob(IJob job)
        {
            Logger.Debug("Queueing job {0}", job.GetUid());

            if (_shutdown) throw new InvalidOperationException("JobManager is shutting down! Come back tomorrow.");

            _allJobs.Add(job);
            lock (_jobsTodo)
            {
                _jobsTodo.Add(job);
            }
            JobAdded?.Invoke(job);

            if (_scheduler != null && _scheduler.IsAlive) return;
            Logger.Debug("Starting scheduler thread");
            _scheduler = new Thread(Schedule);
            _scheduler.Start();
        }

        /// <summary>
        /// Returns all jobs ever queued.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IJob> GetAllJobs() => _allJobs.AsReadOnly();

        /// <summary>
        /// Return all jobs currently running or queued.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IJob> GetActiveJobs() => _allJobs.Where(j => !j.IsDone());

        private WorkUnit GetWork(out IJob parentJob)
        {
            lock (_jobsTodo)
            {
                Logger.Trace("Getting work");
                if (!_jobsTodo.Any())
                {
                    Logger.Trace("No jobs");
                    parentJob = null;
                    return null;
                }

                var jobs = new List<IJob>(_jobsTodo);
                foreach (var job in jobs.OrderBy(j => -j.GetPriority()))
                {
                    Logger.Trace("Checking job {0}", job.GetUid());
                    WorkUnit work = null;
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
                        try
                        {
                            JobRemoved?.Invoke(job);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            throw;
                        }
                    }

                    if (work != null)
                    {
                        Logger.Trace("Got work: {0}", work);
                        parentJob = job;
                        return work;
                    }

                    Logger.Trace("No work. De-queueing job");
                    _jobsTodo.Remove(job);
                    try
                    {
                        JobRemoved?.Invoke(job);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                parentJob = null;
                return null;
            }
        }

        private void DoWork()
        {
            var done = false;
            while (!_shutdown)// && _threadsDone < MAX_THREADS)
            {
                var work = GetWork(out var job);
                if (work == null)
                {
                    if (!done)
                    {
                        done = true;
                        Logger.Trace("Worker thread going to sleep.");
                        lock (_counterLock)
                        {
                            _threadsDone++;
                        }
                    }
                    Thread.Sleep(1000);
                    continue;
                }

                if (done)
                {
                    done = false;
                    Logger.Trace("Worker thread waking up.");
                    lock (_counterLock)
                    {
                        _threadsDone--;
                    }
                }
#if !DEBUG
                try
                {
#endif
                    work.Work();
                    if (!_shutdown) job.WorkItemFinished();
#if !DEBUG
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    work.SetError(e);
                    job.WorkItemFinished();
                }
#endif
            }
            Logger.Trace("Worker thread ending.");
        }

        private void Schedule()
        {
            _threadsDone = 0;
            var threads = new List<Thread>();

            for (int i = 0; i < MAX_THREADS; i++)
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

            Logger.Debug("Scheduler thread ending");
        }

        /// <summary>
        /// Shutdown all threads.
        /// </summary>
        public void Shutdown(bool blocking)
        {
            Logger.Info("JobManager shutting down");
            if (_scheduler == null || !_scheduler.IsAlive) return;
            _shutdown = true;
            lock (_jobsTodo)
            {
                foreach (var job in _jobsTodo)
                {
                    job.Abort();
                }
            }

            if (blocking) _scheduler.Join();
        }
    }
}
