using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Core.Model.Updating
{
    public interface IUpdateState
    {
        public event Action OnEnded;
    }

    public interface IUpdateCreated : IUpdateState
    {
        Task<IUpdatePrepared> Prepare(CancellationToken cancellationToken);
    }

    public interface IUpdatePrepared : IUpdateState
    {
        Task<IUpdateDone> Update(CancellationToken cancellationToken);
    }

    public interface IUpdateDone
    {

    }
}
