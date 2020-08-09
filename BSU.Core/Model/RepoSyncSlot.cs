using System;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.CoreCommon;

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

        public RepoSyncSlot(IJobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public void Prepare(IRepositoryMod repository, StorageMod storage, UpdateTarget target, string title, Action rollback = null)
        {
            if (_state != RepoSyncSlotState.Inactive) throw new InvalidOperationException();

            Target = target;

            _rollback = rollback;
            var name = $"Preparing {storage.Identifier} update";
            
            _prepareJob = new SimpleJob(cancellationToken => DoPrepare(repository, storage, target, cancellationToken), name, 1);
            _prepareJob.OnFinished += () =>
            {
                var error = _prepareJob.Error;
                if (error != null)
                {
                    _state = RepoSyncSlotState.Inactive;
                    Target = null;
                    OnFinished?.Invoke(error);
                    return;
                }
                _state = RepoSyncSlotState.Prepared;
                OnPrepared?.Invoke();
            };
            _state = RepoSyncSlotState.Preparing;
            _jobManager.QueueJob(_prepareJob);
        }

        private void DoPrepare(IRepositoryMod repository, StorageMod storage, UpdateTarget target, CancellationToken cancellationToken)
        {
            var name = $"Updating {storage.Identifier}";
            _syncJob = new RepoSync(repository, storage, target, name, 0, cancellationToken);
        }

        public void Commit()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();
            if (_syncJob.NothingToDo)
            { 
                Finished();
                return;
            }
            _state = RepoSyncSlotState.Updating;
            _syncJob.OnFinished += Finished;
            _jobManager.QueueJob(_syncJob);
        }

        private void Finished()
        {
            _state = RepoSyncSlotState.Inactive;
            Target = null;
            OnFinished?.Invoke(_syncJob.GetError());
        }

        public void Abort()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();

            
            Target = null;
            _state = RepoSyncSlotState.Inactive;
            try
            {
                _rollback?.Invoke();
                OnFinished?.Invoke(null);
            }
            catch (Exception e)
            {
                OnFinished?.Invoke(e);
            }
            
        }

        public bool IsActive() => _state != RepoSyncSlotState.Inactive;

        public event Action OnPrepared;
        public event Action<Exception> OnFinished;
        public int GetPrepStats()
        {
            if (_state != RepoSyncSlotState.Prepared) throw new InvalidOperationException();

            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }

        public bool IsPrepared => _state == RepoSyncSlotState.Prepared;
    }

    internal enum RepoSyncSlotState
    {
        Inactive,
        Preparing,
        Prepared,
        Updating
    }
}