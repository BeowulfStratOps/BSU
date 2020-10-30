using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model.Utility;
using NLog;

namespace BSU.Core.Model
{
    public class RepositoryUpdate
    {
        private readonly SetUpDelegate _setUpCallback;
        private readonly PreparedDelegate _preparedCallback;
        private readonly FinishedDelegate _finishedCallback;
        
        // TODO: create Logger with builtin Uid thing?
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event Action OnEnded;

        private List<IUpdateState> _workingSet = new List<IUpdateState>();
        private UpdateState _nextStage = UpdateState.Created;
        private bool _started;

        public RepositoryUpdate(SetUpDelegate setUpCallback, PreparedDelegate preparedCallback, FinishedDelegate finishedCallback)
        {
            _setUpCallback = setUpCallback;
            _preparedCallback = preparedCallback;
            _finishedCallback = finishedCallback;
            _logger.Info("Creating");
        }

        public ProgressProvider ProgressProvider { get; private set; } = new ProgressProvider();

        internal void Add(IUpdateState updateState)
        {
            _workingSet.Add(updateState);
            updateState.OnStateChange += Check;
            updateState.ProgressProvider.PropertyChanged += (sender, args) => UpdateProgress();
            _logger.Trace("Added updateState with state {0}", updateState.State);
        }

        private void UpdateProgress()
        {
            ProgressProvider.IsIndeterminate = _workingSet.Any(u => u.ProgressProvider.IsIndeterminate);
            ProgressProvider.Value = _workingSet.Sum(u => u.ProgressProvider.Value) / _workingSet.Count;
        }

        private void Check()
        {
            if (!_workingSet.All(u => u.State == _nextStage || u.State == UpdateState.Errored)) return;
            
            _logger.Info("Stage {0} done", _nextStage);

            var succeeded = _workingSet.Where(u => u.State != UpdateState.Errored).ToList();
            var failed = _workingSet.Where(u => u.State == UpdateState.Errored).ToList();
            
            _workingSet = new List<IUpdateState>(succeeded);
            
            var args = new StageCallbackArgs(succeeded, failed);
            
            switch (_nextStage)
            {
                case UpdateState.Created:
                    _nextStage = UpdateState.Prepared;
                    ProgressProvider.Stage = UpdateState.Preparing.ToString();
                    _setUpCallback(args, Callback);
                    break;
                case UpdateState.Prepared:
                    _nextStage = UpdateState.Updated;
                    ProgressProvider.Stage = UpdateState.Updating.ToString();
                    _preparedCallback(args, Callback);
                    break;
                case UpdateState.Updated:
                    _finishedCallback(args);
                    OnEnded?.Invoke();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void Callback(bool decision)
        {
            _logger.Info("Advance to stage {0}: {1}", _nextStage, decision);
            foreach (var updateState in _workingSet)
            {
                if (decision)
                    updateState.Continue();
                else
                    updateState.Abort();
            }
        }
        
        public delegate void SetUpDelegate(StageCallbackArgs args, Action<bool> proceed);
        public delegate void PreparedDelegate(StageCallbackArgs args, Action<bool> proceed);
        public delegate void FinishedDelegate(StageCallbackArgs args);

        public void Start()
        {
            if (_started) throw new InvalidOperationException();
            _logger.Info("Start");
            _started = true;
            
            // None that need to be created first
            if (_workingSet.All(u => u.State != UpdateState.NotCreated))
                _nextStage = UpdateState.Prepared; 
            
            
            
            foreach (var updateState in _workingSet)
            {
                if (_nextStage == UpdateState.Prepared || updateState.State == UpdateState.NotCreated)
                    updateState.Continue();
            }
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
