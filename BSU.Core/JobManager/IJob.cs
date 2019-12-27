using System;
using BSU.Core.Sync;
using BSU.CoreCommon;

namespace BSU.Core.JobManager
{
    internal interface IJob
    {
        Uid GetUid();
        bool IsDone();
        void SetError(Exception e);
        void CheckDone();
        void Abort();
        WorkUnit GetWork();
    }
}
