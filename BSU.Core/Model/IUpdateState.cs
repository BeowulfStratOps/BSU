using System;

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
        
        bool IsIndeterminate { get; }
        double Progress { get; }
        event Action OnProgressChange;
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
