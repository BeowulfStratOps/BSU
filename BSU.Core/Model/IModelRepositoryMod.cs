using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Model
{
    internal interface IModelRepositoryMod : IHashCollection
    {
        public void SetSelection(ModSelection selection);
        string Identifier { get; }
        IModelRepository ParentRepository { get; }
        LoadingState State { get; }
        Task<ModUpdateInfo?> StartUpdate(IProgress<FileSyncStats>? progress, CancellationToken cancellationToken);
        ModInfo GetModInfo();
        ModSelection GetCurrentSelection();
        event Action<IModelRepositoryMod> StateChanged;
        PersistedSelection? GetPreviousSelection();
        event Action<IModelRepositoryMod> SelectionChanged;
    }
}
