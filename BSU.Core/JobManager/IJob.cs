using System;
using BSU.Core.Model;
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
        bool DoWork(IActionQueue actionQueue); // provide action queue to keep events out of threading hell
        string GetTitle();
        int GetPriority();
    }
}
