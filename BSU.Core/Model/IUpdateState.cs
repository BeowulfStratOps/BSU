using System;

namespace BSU.Core.Model
{
    public interface IUpdateState
    {
        void Prepare();
        void Commit();
        void Abort();
        event Action OnPrepared;
        int GetPrepStats();
        bool IsPrepared { get; }
    }
}