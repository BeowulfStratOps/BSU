using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class UpdatePacket
    {
        private readonly Core _core;
        internal readonly List<UpdateJob> Jobs = new List<UpdateJob>();

        public UpdatePacket(Core core)
        {
            _core = core;
        }

        public List<JobView> GetJobsViews() => Jobs.Select(j => new JobView(j)).ToList();

        public void DoUpdate()
        {
            _core.DoUpdate(this);
        }
    }
}
