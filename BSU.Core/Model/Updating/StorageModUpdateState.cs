using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.Model.Updating
{

    internal class StorageModUpdateState : IModUpdate
    {
        private readonly StorageMod _storageMod;
        private readonly IRepositoryMod _repositoryMod;
        private readonly IProgress<FileSyncStats> _progress;

        private RepoSync _repoSync;

        private bool _prepared, _updated;

        public StorageModUpdateState(StorageMod storageMod, IRepositoryMod repositoryMod, IProgress<FileSyncStats> progress)
        {
            _storageMod = storageMod;
            _repositoryMod = repositoryMod;
            _progress = progress;
        }

        public event Action OnEnded;

        public async Task Prepare(CancellationToken cancellationToken)
        {
            if (_prepared) throw new InvalidOperationException();
            _prepared = true;

            try
            {
                _repoSync = await RepoSync.BuildAsync(_repositoryMod, _storageMod, cancellationToken);
                IsPrepared = true;
            }
            catch
            {
                OnEnded?.Invoke();
                _progress.Report(new FileSyncStats(FileSyncState.None, 0, 0, 0, 0));
                throw;
            }
        }

        public async Task Update(CancellationToken cancellationToken)
        {
            if (_updated) throw new InvalidOperationException();
            _updated = true;

            if (_repoSync == null) throw new InvalidOperationException();

            try
            {
                await _repoSync.UpdateAsync(cancellationToken, _progress);
            }
            finally
            {
                OnEnded?.Invoke();
            }
        }
        public bool IsPrepared { get; private set; }
    }
}
