using System;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model
{
    public interface IUpdateState
    {
        void Continue();
        void Abort();
        
        UpdateState State { get; }
        Exception Exception { get; }
        
        event Action OnStateChange;
        event Action OnEnded;
        
        int GetPrepStats();

        IProgressProvider ProgressProvider { get; }
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
