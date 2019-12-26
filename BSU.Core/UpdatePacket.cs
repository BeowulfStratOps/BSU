using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.Core.State;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core
{
    // TODO: make disposable?
    public class UpdatePacket
    {
        private readonly Core _core;
        private readonly State.State _state;
        internal readonly List<UpdateJob> Jobs = new List<UpdateJob>();
        internal readonly List<Action> Rollback = new List<Action>();

        public UpdatePacket(Core core, State.State state)
        {
            _core = core;
            _state = state;
        }

        public List<JobView> GetJobsViews() => Jobs.Select(j => new JobView(j)).ToList();

        public void DoUpdate()
        {
            try
            {
                if (!_state.IsValid) throw new InvalidOperationException("State is invalid!");
                _core.DoUpdate(this);
            }
            catch (Exception)
            {
                Abort();
                throw;
            }
        }

        public void Abort()
        {
            // TODO: inalidate object
            foreach (var action in Rollback)
            {
                action();
            }
        }

        public bool IsDone() => Jobs.All(j => j.SyncState.IsDone());
        public bool HasError() => Jobs.All(j => j.SyncState.HasError());
    }
}
