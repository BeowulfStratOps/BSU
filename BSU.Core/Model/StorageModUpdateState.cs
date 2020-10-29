using System;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class StorageModUpdateState : IUpdateState
    {
        private readonly IJobManager _jobManager;
        private readonly IActionQueue _actionQueue;
        private readonly IRepositoryMod _repositoryMod;
        private readonly Func<IUpdateState, StorageMod> _createStorageMod;
        private StorageMod _storageMod;
        private RepoSync _syncJob;
        
        public UpdateTarget Target { get; }
        public Exception Exception { get; private set; }
        private Action _abort;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        
        
        private UpdateState _state;

        public UpdateState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _state = value;
                OnStateChange?.Invoke();
            }
        }

        public StorageModUpdateState(IJobManager jobManager, IActionQueue actionQueue, IRepositoryMod repositoryMod, StorageMod storageMod, UpdateTarget target)
        {
            _jobManager = jobManager;
            _actionQueue = actionQueue;
            _repositoryMod = repositoryMod;
            _storageMod = storageMod;
            Target = target;
            
            _state = UpdateState.Created;
            
            _logger.Info("Creating Update State for mod {0}", storageMod.Identifier);
        }

        public StorageModUpdateState(IJobManager jobManager, IActionQueue actionQueue, IRepositoryMod repositoryMod, UpdateTarget target, Func<IUpdateState, StorageMod> createStorageMod)
        {
            Target = target;
            _jobManager = jobManager;
            _actionQueue = actionQueue;
            _repositoryMod = repositoryMod;
            _createStorageMod = createStorageMod;

            _state = UpdateState.NotCreated;
            
            _logger.Info("Creating Download State");
        }

        public void Continue()
        {
            switch (State)
            {
                case UpdateState.NotCreated:
                    Create();
                    break;
                case UpdateState.Created:
                    Prepare();
                    break;
                case UpdateState.Prepared:
                    Update();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void Create()
        {
            _logger.Info("Create");
            var job = new SimpleJob(_ =>
            {
                var storageMod = _createStorageMod(this);
                _actionQueue.EnQueueAction(() =>
                {
                    _storageMod = storageMod;
                });
            }, "Create StorageMod", 1);
            job.OnFinished += () =>
            {
                if (job.Error == null)
                    State = UpdateState.Created;
                else
                {
                    Exception = job.Error;
                    State = UpdateState.Errored;
                    OnEnded?.Invoke();
                }
            };
            _abort = () => job.Abort();
            State = UpdateState.Creating;
            _jobManager.QueueJob(job);
        }

        private void Prepare()
        {
            _logger.Info("Create {0}", _storageMod.Identifier);
            var name = $"Preparing {_storageMod.Identifier} update";

            var job = new SimpleJob(DoPrepare, name, 1);
            job.OnFinished += () =>
            {
                if (job.Error == null)
                    State = UpdateState.Prepared;
                else
                {
                    Exception = job.Error;
                    State = UpdateState.Errored;
                    OnEnded?.Invoke();
                }
            };
            _abort = () => job.Abort();
            State = UpdateState.Preparing;
            _jobManager.QueueJob(job);
        }

        private void DoPrepare(CancellationToken cancellationToken)
        {
            var name = $"Updating {_storageMod.Identifier}";
            _syncJob = new RepoSync(_repositoryMod, _storageMod, Target, name, 0, cancellationToken);
        }

        private void Update()
        {
            _logger.Info("Update {0}", _storageMod.Identifier);
            if (_syncJob.NothingToDo)
            {
                State = UpdateState.Updated;
                OnEnded?.Invoke();
                return;
            }
            _syncJob.OnFinished += () =>
            {
                var error = _syncJob.GetError();
                if (error == null)
                    State = UpdateState.Updated;
                else
                {
                    Exception = error;
                    State = UpdateState.Errored;
                }

                OnEnded?.Invoke();
            };
            IsIndeterminate = false;
            _syncJob.OnProgress += () =>
            {
                Progress = _syncJob.GetProgress();
                OnProgressChange?.Invoke();
            };
            _abort = () => _syncJob.Abort();
            State = UpdateState.Updating;
            _jobManager.QueueJob(_syncJob);
        }

        public void Abort()
        {
            if (State == UpdateState.Creating || State == UpdateState.Preparing || State == UpdateState.Updating)
            {
                _abort();
            }
            else
            {
                if (State != UpdateState.Created && State != UpdateState.Prepared) throw  new InvalidOperationException();
            }
            
            State = UpdateState.Aborted;
            OnEnded?.Invoke();
        }

        public event Action OnStateChange;

        public event Action OnEnded;

        public int GetPrepStats()
        {
            if (State != UpdateState.Prepared) throw new InvalidOperationException();

            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }

        public bool IsIndeterminate { get; private set; } = true;
        public double Progress { get; private set; }
        public event Action OnProgressChange;

        public override string ToString()
        {
            return (_storageMod?.Identifier ?? "??") + ": " + State;
        }
    }
}
