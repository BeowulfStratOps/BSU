using System.Collections.Generic;

namespace BSU.Core
{
    internal interface ISyncManager
    {
        IReadOnlyList<UpdateJob> GetAllJobs();
        IReadOnlyList<UpdateJob> GetActiveJobs();
        void QueueJob(UpdateJob job);
    }
}