using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Model.Utility;
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


        private readonly ProgressProvider _progressProvider = new ProgressProvider();

        public UpdateState State { get; private set; }

        public StorageModUpdateState(IJobManager jobManager, IActionQueue actionQueue, IRepositoryMod repositoryMod, StorageMod storageMod, UpdateTarget target)
        {
            _jobManager = jobManager;
            _actionQueue = actionQueue;
            _repositoryMod = repositoryMod;
            _storageMod = storageMod;
            Target = target;
            
            State = UpdateState.Created;
            
            _logger.Info("Creating Update State for mod {0}", storageMod.Identifier);
        }

        public StorageModUpdateState(IJobManager jobManager, IActionQueue actionQueue, IRepositoryMod repositoryMod, UpdateTarget target, Func<IUpdateState, StorageMod> createStorageMod)
        {
            Target = target;
            _jobManager = jobManager;
            _actionQueue = actionQueue;
            _repositoryMod = repositoryMod;
            _createStorageMod = createStorageMod;

            State = UpdateState.NotCreated;
            
            _logger.Info("Creating Download State");
        }

        public async Task Create()
        {
            _logger.Info("Create");
            var job = new SimpleResultJob<StorageMod>(_ => _createStorageMod(this), "Create StorageMod", 1);
            _abort = () => job.Abort();
            State = UpdateState.Creating;
            try
            {
                _storageMod = await job.Do(_jobManager);
                State = UpdateState.Created;
            }
            catch (Exception e)
            {
                Exception = e;
                State = UpdateState.Errored;
            }
        }

        public async Task Prepare()
        {
            _logger.Info("Create {0}", _storageMod.Identifier);
            var name = $"Preparing {_storageMod.Identifier} update";
            var job = new SimpleJob(DoPrepare, name, 1);
            _abort = () => job.Abort();
            State = UpdateState.Preparing;
            try
            {
                await job.Do(_jobManager);
                State = UpdateState.Prepared;
            }
            catch (Exception e)
            {
                Exception = e;
                State = UpdateState.Errored;
            }
        }

        private void DoPrepare(CancellationToken cancellationToken)
        {
            var name = $"Updating {_storageMod.Identifier}";
            _syncJob = new RepoSync(_repositoryMod, _storageMod, Target, name, 0, cancellationToken);
        }

        public async Task Update()
        {
            _logger.Info("Update {0}", _storageMod.Identifier);
            if (_syncJob.NothingToDo)
            {
                State = UpdateState.Updated;
                return;
            }
            _progressProvider.IsIndeterminate = false;
            _syncJob.OnProgress += () =>
            {
                _progressProvider.Value = _syncJob.GetProgress();
            };
            _abort = () => _syncJob.Abort();
            State = UpdateState.Updating;

            try
            {
                await _syncJob.Do(_jobManager);
                State = UpdateState.Updated;
            }
            catch (Exception e)
            {
                
                Exception = e;
                State = UpdateState.Errored;
            }
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
        }

        public int GetPrepStats()
        {
            if (State != UpdateState.Prepared) throw new InvalidOperationException();

            return (int) (_syncJob.GetTotalBytesToDownload() + _syncJob.GetTotalBytesToUpdate());
        }

        public IProgressProvider ProgressProvider => _progressProvider;
        public event Action OnEnded; // TODO: use

        public override string ToString()
        {
            return (_storageMod?.Identifier ?? "??") + ": " + State;
        }
    }
}
