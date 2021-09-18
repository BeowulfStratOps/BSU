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
        Task Prepare(CancellationToken cancellationToken);
        Task Update(CancellationToken cancellationToken);
        bool IsPrepared { get; }
    }
}
