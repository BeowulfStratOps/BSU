using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model.Utility;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    public class RepositoryUpdate
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private List<IUpdateState> _workingSet = new List<IUpdateState>();

        public ProgressProvider ProgressProvider { get; private set; } = new ProgressProvider();
        public event Action OnEnded;

        internal void Add(IUpdateState updateState)
        {
            _workingSet.Add(updateState);
            updateState.ProgressProvider.PropertyChanged += (sender, args) => UpdateProgress();
            _logger.Trace("Added updateState with state {0}", updateState.State);
        }

        private void UpdateProgress()
        {
            ProgressProvider.IsIndeterminate = _workingSet.Any(u => u.ProgressProvider.IsIndeterminate);
            ProgressProvider.Value = _workingSet.Sum(u => u.ProgressProvider.Value) / _workingSet.Count;
        }

        public async Task<StageCallbackArgs> Create()
        {
            // TODO: check current state
            var tasks = _workingSet.Where(s => s.State == UpdateState.NotCreated).Select(s => s.Create());
            await Task.WhenAll(tasks);
            // TODO: use tasks for this?
            var ret = new StageCallbackArgs(_workingSet.Where(s => s.Exception == null).ToList(), _workingSet.Where(s => s.Exception != null).ToList());
            _workingSet = _workingSet.Where(s => s.Exception == null).ToList();
            return ret;
        }

        public async Task<StageCallbackArgs> Prepare()
        {
            // TODO: check current state
            var tasks = _workingSet.Where(s => s.State == UpdateState.Created).Select(s => s.Prepare());
            await Task.WhenAll(tasks);
            // TODO: use tasks for this?
            var ret = new StageCallbackArgs(_workingSet.Where(s => s.Exception == null).ToList(), _workingSet.Where(s => s.Exception != null).ToList());
            _workingSet = _workingSet.Where(s => s.Exception == null).ToList();
            return ret;
        }

        public async Task<StageCallbackArgs> Update()
        {
            // TODO: check current state
            var tasks = _workingSet.Where(s => s.State == UpdateState.Prepared).Select(s => s.Update());
            await Task.WhenAll(tasks);
            // TODO: use tasks for this?
            var ret = new StageCallbackArgs(_workingSet.Where(s => s.Exception == null).ToList(), _workingSet.Where(s => s.Exception != null).ToList());
            _workingSet = _workingSet.Where(s => s.Exception == null).ToList();
            OnEnded?.Invoke();
            return ret;
        }

        public void Abort()
        {
            // TODO: set state
            foreach (var updateState in _workingSet)
            {
                updateState.Abort();
            }
            OnEnded?.Invoke();
        }
    }

    public class StageCallbackArgs
    {
        public List<IUpdateState> Succeeded { get; }
        public List<IUpdateState> Failed { get; }

        public StageCallbackArgs(List<IUpdateState> succeeded, List<IUpdateState> failed)
        {
            Succeeded = succeeded;
            Failed = failed;
        }
    }
}
