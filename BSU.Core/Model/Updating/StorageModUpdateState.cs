using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model.Utility;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model.Updating
{
    internal class StorageModUpdateStateCommon
    {
        public readonly MatchHash MatchHash;
        public readonly VersionHash VersionHash;
        public readonly ProgressProvider ProgressProvider = new();
        public readonly IJobManager JobManager;

        public StorageModUpdateStateCommon(MatchHash matchHash, VersionHash versionHash, IJobManager jobManager)
        {
            MatchHash = matchHash;
            VersionHash = versionHash;
            JobManager = jobManager;
        }

        public event Action OnEnded;

        public void SignalEnded()
        {
            OnEnded?.Invoke();
        }
    }

    internal abstract class StorageModUpdateStateBase : IUpdateState
    {
        protected readonly StorageModUpdateStateCommon Common;
        private bool _stateInvalidated;

        protected StorageModUpdateStateBase(StorageModUpdateStateCommon common)
        {
            Common = common;
        }

        protected void InvalidateState()
        {
            if (_stateInvalidated) throw new InvalidOperationException();
            _stateInvalidated = true;
        }

        public IProgressProvider ProgressProvider => Common.ProgressProvider;
        protected ProgressProvider Progress => Common.ProgressProvider;
        public MatchHash GetTargetMatch() => Common.MatchHash;

        public VersionHash GetTargetVersion() => Common.VersionHash;

        public abstract void Abort();

        public event Action OnEnded
        {
            add => Common.OnEnded += value;
            remove => Common.OnEnded -= value;
        }

        protected IJobManager JobManager => Common.JobManager;

        protected void SignalEnded() => Common.SignalEnded();
    }

    internal class StorageModUpdateState : StorageModUpdateStateBase, IUpdateCreate
    {
        private readonly IRepositoryMod _repositoryMod;
        private readonly UpdateTarget _target;

        private readonly Func<IUpdateCreate, StorageMod> _createStorageMod;
        private readonly StorageMod _storageMod;

        private SimpleResultJob<StorageMod> _job;

        private readonly Logger _logger = EntityLogger.GetLogger();

        public StorageModUpdateState(IJobManager jobManager, IRepositoryMod repositoryMod, StorageMod storageMod, UpdateTarget target, MatchHash matchHash, VersionHash versionHash) : base(new StorageModUpdateStateCommon(matchHash, versionHash, jobManager))
        {
            _repositoryMod = repositoryMod;
            _storageMod = storageMod;
            _target = target;

            _logger.Info("Creating Update State for mod {0}", storageMod.Identifier);
        }

        public StorageModUpdateState(IJobManager jobManager, IRepositoryMod repositoryMod, UpdateTarget target, Func<IUpdateCreate, StorageMod> createStorageMod, MatchHash matchHash, VersionHash versionHash) : base(new StorageModUpdateStateCommon(matchHash, versionHash, jobManager))
        {
            _repositoryMod = repositoryMod;
            _target = target;
            _createStorageMod = createStorageMod;

            _logger.Info("Creating Download State");
        }

        public async Task<IUpdateCreated> Create()
        {
            InvalidateState();

            if (_storageMod != null)
                return new StorageModUpdateCreated(_storageMod, _repositoryMod, _target, Common);

            _job = new SimpleResultJob<StorageMod>(_ => _createStorageMod(this), "Create StorageMod", 1);

            try
            {
                var storageMod = await _job.Do(JobManager);
                return new StorageModUpdateCreated(storageMod, _repositoryMod, _target, Common);
            }
            catch
            {
                SignalEnded();
                throw;
            }
        }

        public override void Abort()
        {
            InvalidateState();
            _job?.Abort();
            _job = null;
        }
    }

    internal class StorageModUpdateCreated : StorageModUpdateStateBase, IUpdateCreated
    {
        private readonly StorageMod _storageMod;
        private readonly IRepositoryMod _repositoryMod;
        private readonly UpdateTarget _target;

        private SimpleAsyncJob _job;
        private RepoSync _sync;

        public StorageModUpdateCreated(StorageMod storageMod, IRepositoryMod repositoryMod, UpdateTarget target, StorageModUpdateStateCommon common) : base(common)
        {
            _storageMod = storageMod;
            _repositoryMod = repositoryMod;
            _target = target;
        }

        public override void Abort()
        {
            InvalidateState();

            _job?.Abort();
            _job = null;
            SignalEnded();
        }

        public async Task<IUpdatePrepared> Prepare()
        {
            InvalidateState();

            var name = $"Preparing {_storageMod.Identifier} update";
            _job = new SimpleAsyncJob(DoPrepare, name, 1);

            try
            {
                await _job.Do(JobManager);
                return new StorageModUpdatePrepared(_sync, Common);
            }
            catch
            {
                SignalEnded();
                throw;
            }
        }

        private void DoPrepare(CancellationToken cancellationToken)
        {
            var name = $"Updating {_storageMod.Identifier}";
            _sync = new RepoSync(_repositoryMod, _storageMod, _target, name, 0, cancellationToken);
        }
    }

    internal class StorageModUpdatePrepared : StorageModUpdateStateBase, IUpdatePrepared
    {
        private RepoSync _syncJob;

        public StorageModUpdatePrepared(RepoSync syncJob, StorageModUpdateStateCommon common) : base(common)
        {
            _syncJob = syncJob;
        }

        public override void Abort()
        {
            InvalidateState();
            _syncJob?.Abort();
            _syncJob = null;

            SignalEnded();
        }

        public async Task<IUpdateDone> Update()
        {
            InvalidateState();

            if (_syncJob.NothingToDo)
            {
                SignalEnded();
                return new StorageModUpdateDone();
            }
            Progress.IsIndeterminate = false;
            _syncJob.OnProgress += () =>
            {
                Progress.Value = _syncJob.GetProgress();
            };

            try
            {
                await _syncJob.Do(JobManager);
                return new StorageModUpdateDone();
            }
            finally
            {
                SignalEnded();
            }
        }
        public int GetStats()
        {
            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }
    }

    internal class StorageModUpdateDone : IUpdateDone
    {

    }
}
