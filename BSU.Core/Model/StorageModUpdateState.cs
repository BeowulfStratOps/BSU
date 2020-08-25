using System;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class StorageModUpdateState : IUpdateState
    {
        private readonly IJobManager _jobManager;
        private readonly IRepositoryMod _repository;
        private readonly StorageMod _storage;
        private RepoSync _syncJob;
        private SimpleJob _prepareJob;
        private UpdateStateEnum _state = UpdateStateEnum.Inactive;
        private readonly Action _rollback;

        internal UpdateTarget Target { get; private set; }

        public StorageModUpdateState(IJobManager jobManager, IRepositoryMod repository, StorageMod storage, UpdateTarget target, Action rollback = null)
        {
            Target = target;
            _jobManager = jobManager;
            _repository = repository;
            _storage = storage;
            _rollback = rollback;
        }

        public void Prepare()
        {
            if (_state != UpdateStateEnum.Inactive) throw new InvalidOperationException();

            var name = $"Preparing {_storage.Identifier} update";

            _prepareJob = new SimpleJob(DoPrepare, name, 1);
            _prepareJob.OnFinished += () =>
            {
                var error = _prepareJob.Error;
                if (error != null)
                {
                    _state = UpdateStateEnum.Done;
                    IsFinished = true;
                    Target = null;
                    OnFinished?.Invoke(error);
                    return;
                }
                _state = UpdateStateEnum.Prepared;
                OnPrepared?.Invoke();
            };
            _state = UpdateStateEnum.Preparing;
            _jobManager.QueueJob(_prepareJob);
        }

        private void DoPrepare(CancellationToken cancellationToken)
        {
            var name = $"Updating {_storage.Identifier}";
            _syncJob = new RepoSync(_repository, _storage, Target, name, 0, cancellationToken);
        }

        public void Commit()
        {
            if (_state != UpdateStateEnum.Prepared) throw new InvalidOperationException();
            if (_syncJob.NothingToDo)
            {
                Finished();
                return;
            }
            _state = UpdateStateEnum.Updating;
            _syncJob.OnFinished += Finished;
            _jobManager.QueueJob(_syncJob);
        }

        private void Finished()
        {
            _state = UpdateStateEnum.Done;
            IsFinished = true;
            Target = null;
            OnFinished?.Invoke(_syncJob.GetError());
        }

        public void Abort()
        {
            if (_state != UpdateStateEnum.Prepared) throw new InvalidOperationException();

            Target = null;
            _state = UpdateStateEnum.Inactive;
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

        public event Action OnPrepared;
        public event Action<Exception> OnFinished;

        public int GetPrepStats()
        {
            if (_state != UpdateStateEnum.Prepared) throw new InvalidOperationException();

            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }

        public bool IsPrepared => _state == UpdateStateEnum.Prepared;
        public bool IsFinished { get; private set; }
    }

    internal enum UpdateStateEnum
    {
        Inactive,
        Preparing,
        Prepared,
        Updating,
        Done
    }
}
