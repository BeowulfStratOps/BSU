using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.Model.Updating
{
    internal class StorageModUpdateStateCommon
    {
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

        public event Action OnEnded
        {
            add => Common.OnEnded += value;
            remove => Common.OnEnded -= value;
        }
    }

    internal class StorageModUpdateState : StorageModUpdateStateBase, IUpdateCreated
    {
        private readonly StorageMod _storageMod;
        private readonly IRepositoryMod _repositoryMod;

        public StorageModUpdateState(StorageMod storageMod, IRepositoryMod repositoryMod) : base(new StorageModUpdateStateCommon())
        {
            _storageMod = storageMod;
            _repositoryMod = repositoryMod;
        }

        public async Task<IUpdatePrepared> Prepare(CancellationToken cancellationToken)
        {
            InvalidateState();

            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationToken);
                var repoSync = await RepoSync.BuildAsync(_repositoryMod, _storageMod, cts.Token);
                return new StorageModUpdatePrepared(repoSync, Common);
            }
            finally
            {
                Common.SignalEnded();
            }
        }
    }

    internal class StorageModUpdatePrepared : StorageModUpdateStateBase, IUpdatePrepared
    {
        private readonly RepoSync _syncJob;

        public StorageModUpdatePrepared(RepoSync syncJob, StorageModUpdateStateCommon common) : base(common)
        {
            _syncJob = syncJob;
        }

        public async Task<IUpdateDone> Update(CancellationToken cancellationToken)
        {
            InvalidateState();

            try
            {
                await _syncJob.UpdateAsync(cancellationToken);
                return new StorageModUpdateDone();
            }
            finally
            {
                Common.SignalEnded();
            }
        }
    }

    internal class StorageModUpdateDone : IUpdateDone
    {

    }
}
