using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.Core.State;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core
{
    public class UpdatePacket : IDisposable
    {
        private readonly Core _core;
        private readonly State.State _state;
        internal readonly List<IJobFacade> Jobs = new List<IJobFacade>();
        internal readonly List<Action> Rollback = new List<Action>();
        private bool _aborted = false;

        public UpdatePacket(Core core, State.State state)
        {
            _core = core;
            _state = state;
        }

        public IReadOnlyList<IJobFacade> GetJobsViews() => new List<IJobFacade>(Jobs).AsReadOnly();

        public void DoUpdate()
        {
            if (!_state.IsValid || _aborted) throw new InvalidOperationException("State is invalid!");
                _core.DoUpdate(this);
        }

        private void Abort()
        {
            if (_aborted) return;
            _aborted = true;
            foreach (var action in Rollback)
            {
                action();
            }
        }

        public bool IsDone() => Jobs.All(j => j.IsDone());
        public bool HasError() => Jobs.All(j => j.HasError());

        public void Dispose()
        {
            Abort();
        }
    }
}
