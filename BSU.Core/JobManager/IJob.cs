using System;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.JobManager
{
    /// <summary>
    /// Job, as managed by a JobManager
    /// </summary>
    internal interface IJob
    {
        Uid GetUid();

        void Abort();
        bool DoWork();
        string GetTitle();
        int GetPriority();
        public event Action OnFinished;
    }
}
