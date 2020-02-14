using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core.View
{
    public class JobHelper
    {
        private readonly List<IJobSlot> _slots;

        internal JobHelper(params IJobSlot[] jobSlots)
        {
            _slots = jobSlots.ToList();
            foreach (var jobSlot in _slots)
            {
                jobSlot.OnStarted += () => OnJobStarted?.Invoke();
                jobSlot.OnFinished += () => OnJobEnded?.Invoke();
            }
        }

        public bool HasActiveJobs() => _slots.Any(s => s.IsActive());

        public event Action OnJobStarted, OnJobEnded;
    }
}
