using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model.Updating
{

    internal class StorageModUpdateState : IModUpdate
    {
        private readonly StorageMod _storageMod;
        private readonly IRepositoryMod _repositoryMod;
        private readonly IProgress<FileSyncStats> _progress;
        private readonly ILogger _logger;
        private readonly Guid _guid = Guid.NewGuid();

        private RepoSync _repoSync;

        private bool _prepared, _updated;

        public StorageModUpdateState(StorageMod storageMod, IRepositoryMod repositoryMod, IProgress<FileSyncStats> progress)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, _guid.ToString());
            _storageMod = storageMod;
            _repositoryMod = repositoryMod;
            _progress = progress;
        }

        public event Action OnEnded;

        public async Task Prepare(CancellationToken cancellationToken)
        {
            if (_prepared) throw new InvalidOperationException();
            _prepared = true;

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            try
            {
                _repoSync = await Task.Run(() =>
                    RepoSync.BuildAsync(_repositoryMod, _storageMod, cancellationToken, _guid), cancellationToken);
                IsPrepared = true;
            }
            catch
            {
                ReportProgress(new FileSyncStats(FileSyncState.None));
                OnEnded?.Invoke();
                throw;
            }
        }

        private void ReportProgress(FileSyncStats stats)
        {
            _logger.Trace($"Progress: {stats.State}");
            _progress?.Report(stats);
        }

        public async Task Update(CancellationToken cancellationToken)
        {
            if (_updated) throw new InvalidOperationException();
            _updated = true;

            if (_repoSync == null) throw new InvalidOperationException();

            cancellationToken.Register(() => ReportProgress(new FileSyncStats(FileSyncState.Stopping)));

            try
            {
                await Task.Run(() => _repoSync.UpdateAsync(cancellationToken, _progress), cancellationToken);
            }
            finally
            {
                ReportProgress(new FileSyncStats(FileSyncState.None));
                OnEnded?.Invoke();
            }
        }
        public bool IsPrepared { get; private set; }
    }
}
