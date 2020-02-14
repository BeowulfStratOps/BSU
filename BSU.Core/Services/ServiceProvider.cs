using System;
using System.Collections.Generic;
using System.Text;
using BSU.Core.JobManager;

namespace BSU.Core.Services
{
    static class ServiceProvider
    {
        public static readonly JobManager.JobManager JobManager = new JobManager.JobManager();
        public static InternalState InternalState;
    }
}
