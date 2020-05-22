using System;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.Core.View;

namespace BSU.Core.Model
{
    internal class RepoSyncSlot : IUpdateState
    {
        private readonly IJobManager _jobManager;
        private RepoSync _syncJob;
        private SimpleJob _prepareJob;
        private RepoSyncSlotState _state = RepoSyncSlotState.Inactive;
        private Action _rollback;
        
        internal UpdateTarget Target { get; private set; }
        
        // TODO: add lock

        public RepoSyncSlot(IJobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public void Prepare(RepositoryMod repository, StorageMod storage, UpdateTarget target, string title, Action rollback = null)
        {
            if (_state != RepoSyncSlotState.Inactive) throw new InvalidOperationException();

            Target = target;

            _rollback = rollback;
            var name = $"Preparing {storage.Identifier} update";
            
            _prepareJob = new SimpleJob(() => DoPrepare(repository, storage, target), name, 1);
            _prepareJob.OnFinished += () =>
            {
                _state = RepoSyncSlotState.Prepared;
                OnPrepared?.Invoke();
            };
            _state = RepoSyncSlotState.Preparing;
            _jobManager.QueueJob(_prepareJob);
        }

        private void DoPrepare(RepositoryMod repository, StorageMod storage, UpdateTarget target)
        {
            
            var name = $"Updating {storage.Identifier}";
            _syncJob = new RepoSync(repository, storage, target, name, 0);
        }

        public void Commit()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();
            _state = RepoSyncSlotState.Updating;
            _syncJob.OnFinished += () =>
            {
                _state = RepoSyncSlotState.Inactive;
                Target = null;
                OnFinished?.Invoke();
            };
            _jobManager.QueueJob(_syncJob);
        }

        public void Abort()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();

            _rollback?.Invoke();
            Target = null;
            _state = RepoSyncSlotState.Inactive;
        }

        public bool IsActive() => _state != RepoSyncSlotState.Inactive;

        public event Action OnFinished, OnPrepared;
        public int GetPrepStats()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();

            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }
    }

    internal enum RepoSyncSlotState
    {
        Inactive,
        Preparing,
        Prepared,
        Updating
    }
}