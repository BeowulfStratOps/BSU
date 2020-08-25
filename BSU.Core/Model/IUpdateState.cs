using System;

namespace BSU.Core.Model
{
    public interface IUpdateState
    {
        // TODO: Improve interface. use either events with state information, or state from the object. not both

        void Prepare();
        void Commit();
        void Abort();
        event Action OnPrepared;
        event Action<Exception> OnFinished;
        int GetPrepStats();
        bool IsPrepared { get; }
        bool IsFinished { get; }
    }
}
