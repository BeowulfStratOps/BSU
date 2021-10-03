using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.ViewModel;

namespace BSU.Core.Model.Updating
{
    internal interface IRepositoryUpdate
    {
        Task<StageStats> Prepare(CancellationToken cancellationToken);
        Task<StageStats> Update(CancellationToken cancellationToken);
    }

    internal interface IModUpdate
    {
        Task<UpdateResult> Prepare(CancellationToken cancellationToken);
        Task<UpdateResult> Update(CancellationToken cancellationToken);
        bool IsPrepared { get; }
        IModelStorageMod GetStorageMod();
    }

    internal enum UpdateResult
    {
        Success,
        Failed,
        FailedSharingViolation
    }
}
