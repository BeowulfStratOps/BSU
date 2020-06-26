using System;

namespace BSU.Core.Sync
{
    internal interface IJobProgress
    {
        event Action OnProgress;
        float GetProgress();
    }
}