using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Sync
{
    /// <summary>
    /// WorkUnit: Updates a single file from a remote location. Refers to the RepositoryMod for the actual operation.
    /// </summary>
    internal class UpdateAction : SyncWorkUnit
    {
       private readonly IRepositoryMod _repository;

        public UpdateAction(IRepositoryMod repository, StorageMod storage, string path)
            : base(storage, path)
        {
            _repository = repository;
        }

        public override async Task DoAsync(CancellationToken cancellationToken)
        {
            Logger.Trace("{0}, {1} Updating {2}", Storage, _repository, Path);
            await using var target = await Storage.Implementation.OpenFile(Path.ToLowerInvariant(), FileAccess.ReadWrite, cancellationToken);
            await _repository.UpdateTo(Path, target, null, cancellationToken);
        }
    }
}
