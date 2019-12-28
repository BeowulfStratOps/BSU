using System;
using System.Collections.Generic;
using System.Linq;

namespace BSU.Core
{
    /// <summary>
    /// Collection of jobs that are started, if this update is committed to.
    /// Should be used in a using block.
    /// </summary>
    public class UpdatePacket : IDisposable
    {
        private readonly Core _core;
        private readonly State.State _state;
        internal readonly List<IJobFacade> Jobs = new List<IJobFacade>();
        internal readonly List<Action> Rollback = new List<Action>();
        private bool _aborted;

        internal UpdatePacket(Core core, State.State state)
        {
            _core = core;
            _state = state;
        }

        /// <summary>
        /// Jobs that will be started as part of this update.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IJobFacade> GetJobs() => new List<IJobFacade>(Jobs).AsReadOnly();

        /// <summary>
        /// Start the update.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// Check whether the update is done.
        /// </summary>
        /// <returns></returns>
        public bool IsDone() => Jobs.All(j => j.IsDone());

        /// <summary>
        /// Check whether an error occured in one of the jobs.
        /// </summary>
        /// <returns></returns>
        public bool HasError() => Jobs.All(j => j.HasError());

        public void Dispose()
        {
            Abort();
        }
    }
}
