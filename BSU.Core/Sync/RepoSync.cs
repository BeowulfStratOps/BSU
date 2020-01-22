using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.JobManager;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// Job for updating/downloading a storage mod from a repository.
    /// </summary>
    internal class RepoSync : IJob, IJobFacade
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<WorkUnit> _allActions, _actionsTodo;
        private Exception _error;

        private readonly Uid _uid = new Uid();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal readonly IStorageMod StorageMod;
        internal readonly IRepositoryMod RepositoryMod;
        internal readonly UpdateTarget Target;
        private ReferenceCounter _workCounter = new ReferenceCounter();

        public Uid GetUid() => _uid;

        internal CancellationToken GetCancellationToken() => _cancellationTokenSource.Token;

        public RepoSync(IRepositoryMod repository, IStorageMod storage, UpdateTarget target)
        {
            StorageMod = storage;
            Target = target;
            RepositoryMod = repository;

            Logger.Debug("Building sync actions {0} to {1}: {2}", storage.GetUid(), repository.GetUid(), _uid);

            _allActions = new List<WorkUnit>();
            var repositoryList = repository.GetFileList();
            var storageList = storage.GetFileList();
            var storageListCopy = new List<string>(storageList);
            foreach (var repoFile in repositoryList)
            {
                if (storageList.Contains(repoFile))
                {
                    if (!repository.GetFileHash(repoFile).Equals(storage.GetFileHash(repoFile)))
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

            _actionsTodo = new List<WorkUnit>(_allActions);
            Logger.Debug("Download actions: {0}", _actionsTodo.OfType<DownloadAction>().Count());
            Logger.Debug("Update actions: {0}", _actionsTodo.OfType<UpdateAction>().Count());
            Logger.Debug("Delete actions: {0}", _actionsTodo.OfType<DeleteAction>().Count());
        }


        public long GetRemainingBytesToDownload() =>
            _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesRemaining());

        public long GetRemainingBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesRemaining());

        public int GetRemainingChangedFilesCount() => _allActions.OfType<UpdateAction>().Count(a => !a.IsDone());

        public int GetRemainingDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count(a => !a.IsDone());

        public string GetStorageModDisplayName() => StorageMod.GetDisplayName();

        public string GetRepositoryModDisplayName() => RepositoryMod.GetDisplayName();

        public int GetRemainingNewFilesCount() => _allActions.OfType<DownloadAction>().Count(a => !a.IsDone());
        public long GetTotalBytesToDownload() => _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesTotal());

        public long GetTotalBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesTotal());

        public int GetTotalChangedFilesCount() => _allActions.OfType<UpdateAction>().Count();

        public int GetTotalDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count();

        public int GetTotalNewFilesCount() => _allActions.OfType<DownloadAction>().Count();

        /// <summary>
        /// Grabs an atomic piece of work.
        /// </summary>
        /// <returns></returns>
        public WorkUnit GetWork()
        {
            if (_cancellationTokenSource.IsCancellationRequested) return null;
            var work = _actionsTodo.FirstOrDefault();
            if (work != null) _actionsTodo.Remove(work);
            _workCounter.Inc();
            return work;
        }

        /// <summary>
        /// Determines whether this job is completed/errored/aborted.
        /// </summary>
        /// <returns></returns>
        public bool IsDone() =>
            _cancellationTokenSource.IsCancellationRequested || HasError() ||
            _allActions.All(a =>
                a.IsDone() ||
                a.HasError()); // TODO: wait for job to be fully canceled OR split IsDone into more meaningful parts

        public string GetTargetHash() => Target.Hash;

        /// <summary>
        /// Triggered when <see cref="IsDone"/> becomes true.
        /// </summary>
        public event IJobFacade.JobEndedDelegate JobEnded;

        public void SetError(Exception e) => _error = e;

        /// <summary>
        /// Returns whether an error happened during the execution of this job.
        /// </summary>
        /// <returns></returns>
        public bool HasError()
        {
            if (_error != null) return true;
            return _allActions.Any(a => a.HasError());
        }

        /// <summary>
        /// Returns an error that happened during execution, or null.
        /// </summary>
        /// <returns></returns>
        public Exception GetError()
        {
            if (_error != null) return _error;
            return _allActions.FirstOrDefault(a => a.HasError())?.GetError();
        }

        /// <summary>
        /// Signals to abort this job. Does not wait.
        /// </summary>
        public void Abort() => _cancellationTokenSource.Cancel();

        /// <summary>
        /// Check if the job is finished. Necessary due to poor work-unit tracking :shrug:
        /// </summary>
        public void WorkItemFinished()
        {
            _workCounter.Dec();
            if (!_workCounter.Done) return;
            Logger.Debug("Sync done");
            JobEnded?.Invoke(!HasError());
        }
    }
}
