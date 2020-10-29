using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Job for updating/downloading a storage mod from a repository.
    /// </summary>
    internal class RepoSync : IJob, IJobProgress
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<SyncWorkUnit> _allActions, _actionsTodo;
        private readonly List<Exception> Errors = new List<Exception>();

        private readonly Uid _uid = new Uid();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal readonly StorageMod StorageMod;
        internal readonly IRepositoryMod RepositoryMod;
        internal readonly UpdateTarget Target;
        private readonly string _title;
        private readonly int _priority;
        private readonly ReferenceCounter _workCounter;
        private readonly int _totalCount;
        public readonly bool NothingToDo;

        public Uid GetUid() => _uid;

        public RepoSync(IRepositoryMod repository, StorageMod storage, UpdateTarget target, string title, int priority, CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            // TODO: write some tests!
            StorageMod = storage;
            Target = target;
            _title = title;
            _priority = priority;
            RepositoryMod = repository;

            Logger.Debug("Building sync actions {0} to {1}: {2}", storage.Uid, repository.GetUid(), _uid);

            _allActions = new List<SyncWorkUnit>();
            var repositoryList = repository.GetFileList();
            var storageList = storage.Implementation.GetFileList();
            var storageListCopy = new List<string>(storageList);
            foreach (var repoFile in repositoryList)
            {
                if (storageList.Contains(repoFile))
                {
                    if (!repository.GetFileHash(repoFile).Equals(storage.Implementation.GetFileHash(repoFile)))
                    {
                        _allActions.Add(new UpdateAction(repository, storage, repoFile,
                            repository.GetFileSize(repoFile), this));
                    }

                    storageListCopy.Remove(repoFile);
                }
                else
                {
                    _allActions.Add(new DownloadAction(repository, storage, repoFile, repository.GetFileSize(repoFile),
                        this));
                }
            }

            foreach (var storageModFile in storageListCopy)
            {
                _allActions.Add(new DeleteAction(storage, storageModFile, this));
            }

            _actionsTodo = new List<SyncWorkUnit>(_allActions);
            Logger.Debug("Download actions: {0}", _actionsTodo.OfType<DownloadAction>().Count());
            Logger.Debug("Update actions: {0}", _actionsTodo.OfType<UpdateAction>().Count());
            Logger.Debug("Delete actions: {0}", _actionsTodo.OfType<DeleteAction>().Count());
            
            _totalCount = _actionsTodo.Count;
            _workCounter = new ReferenceCounter(_actionsTodo.Count);

            NothingToDo = _totalCount == 0;
        }


        public long GetRemainingBytesToDownload() =>
            _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesRemaining());

        public long GetRemainingBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesRemaining());

        public int GetRemainingChangedFilesCount() => _allActions.OfType<UpdateAction>().Count(a => !a.IsDone());

        public int GetRemainingDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count(a => !a.IsDone());

        public string GetStorageModDisplayName() => StorageMod.Implementation.GetDisplayName();

        public string GetRepositoryModDisplayName() => RepositoryMod.GetDisplayName();

        public int GetRemainingNewFilesCount() => _allActions.OfType<DownloadAction>().Count(a => !a.IsDone());
        public long GetTotalBytesToDownload() => _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesTotal());

        public long GetTotalBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesTotal());

        public int GetTotalChangedFilesCount() => _allActions.OfType<UpdateAction>().Count();

        public int GetTotalDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count();

        public int GetTotalNewFilesCount() => _allActions.OfType<DownloadAction>().Count();

        public bool DoWork(IActionQueue actionQueue)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return false;
            SyncWorkUnit work;
            lock (_actionsTodo)
            {
                if (_actionsTodo.Count == 0) return false;
                work = _actionsTodo[0];
                _actionsTodo.Remove(work);
            }

            try
            {
                work.Work(_cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Errors.Add(e);
                Logger.Error(e);
            }

            actionQueue.EnQueueAction(() => OnProgress?.Invoke());
            if (_workCounter.Dec())
            {
                Logger.Debug("Sync done");
                actionQueue.EnQueueAction(() => OnFinished?.Invoke());
            }
            
            lock (_actionsTodo)
            {
                return _actionsTodo.Count > 0;
            }
        }

        public string GetTargetHash() => Target.Hash;

        /// <summary>
        /// Signals to abort this job. Does not wait.
        /// </summary>
        public void Abort(bool coldShutdown = false)
        {
            _cancellationTokenSource.Cancel();
            if (!coldShutdown)
                OnFinished?.Invoke();
        }

        public Exception GetError()
        {
            return Errors.Any() ? new CombinedException(Errors) : null;
        }

        public event Action OnFinished;

        public string GetTitle() => _title;

        public event Action OnProgress;

        public float GetProgress() => (_totalCount - _workCounter.Remaining) / (float) _totalCount;
        
        public int GetPriority() => _priority;
    }

    internal class CombinedException : Exception
    {
        public readonly List<Exception> Exceptions;

        public CombinedException(List<Exception> exceptions)
        {
            Exceptions = exceptions;
        }
    }
}
