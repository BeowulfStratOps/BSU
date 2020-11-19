using System;
using System.Threading.Tasks;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model
{
    public interface IUpdateState
    {
        Task Create();
        Task Prepare();
        Task Update();
        void Abort();
        
        UpdateState State { get; }
        Exception Exception { get; }
        
        int GetPrepStats();

        IProgressProvider ProgressProvider { get; }

        event Action OnEnded;
    }

    public enum UpdateState
    {
        NotCreated,
        Creating,
        Created,
        Preparing,
        Prepared,
        Updating,
        Updated,
        Aborted,
        Errored
    }
}
