using System;

namespace BSU.Core.Model
{
    internal interface IUpdateState
    {
        void Commit();
        void Abort();
        event Action OnPrepared;
    }
}