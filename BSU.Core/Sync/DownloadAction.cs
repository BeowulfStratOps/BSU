using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Downloads a new file. Refers to the RepositoryMod for the actual download.
    /// </summary>
    internal class DownloadAction : SyncWorkUnit
    {
        private readonly IRepositoryMod _repository;

        public DownloadAction(IRepositoryMod repository, StorageMod storage, string path) : base(storage, path)
        {
            _repository = repository;
        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace("{0}, {1} Downloading {2}", _repository, _repository, Path);
            await using var target = await Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.Write, cancellationToken);
            await _repository.DownloadTo(Path, target, null, cancellationToken);
        }
    }
}
