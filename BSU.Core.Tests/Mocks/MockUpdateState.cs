using System;
using BSU.Core.Model;
using BSU.Core.Model.Utility;
using BSU.Core.ViewModel;

namespace BSU.Core.Tests.Mocks
{
    internal class MockUpdateState : IUpdateState
    {
        private readonly bool _errorCreate;
        private readonly bool _errorPrepare;
        private readonly bool _errorUpdate;

        public bool CommitCalled, AbortCalled;

        public MockUpdateState(bool errorPrepare, bool errorUpdate)
        {
            _errorPrepare = errorPrepare;
            _errorUpdate = errorUpdate;
            _state = UpdateState.Created;
        }
        
        public MockUpdateState(bool errorCreate, bool errorPrepare, bool errorUpdate)
        {
            _errorCreate = errorCreate;
            _errorPrepare = errorPrepare;
            _errorUpdate = errorUpdate;
            _state = UpdateState.NotCreated;
        }

        public void Continue()
        {
            switch (State)
            {
                case UpdateState.NotCreated:
                    if (_errorCreate)
                        Error();
                    else
                        State = UpdateState.Created;
                    break;
                case UpdateState.Created:
                    if (_errorPrepare)
                        Error();
                    else
                        State = UpdateState.Prepared;
                    break;
                case UpdateState.Prepared:
                    CommitCalled = true;
                    if (_errorUpdate)
                        Error();
                    else
                        State = UpdateState.Updated;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void Error()
        {
            State = UpdateState.Errored;
            Exception = new TestException();
            OnEnded?.Invoke();
        }

        public void Abort()
        {
            if (State != UpdateState.Prepared && State != UpdateState.Created && State != UpdateState.Creating &&
                State != UpdateState.Preparing && State != UpdateState.Updating) throw new InvalidOperationException();
            AbortCalled = true;
            State = UpdateState.Aborted;
            OnEnded?.Invoke();
        }

        private UpdateState _state;

        public UpdateState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _state = value;
                OnStateChange?.Invoke();
            }
        }

        public Exception Exception { get; private set; }
        public event Action OnStateChange;
        public event Action OnEnded;

        public int GetPrepStats()
        {
            throw new NotImplementedException();
        }

        public IProgressProvider ProgressProvider { get; } = new ProgressProvider();

        public bool IsIndeterminate { get; }
        public double Progress { get; }

        public event Action OnProgressChange;

        public override string ToString() => State.ToString();
    }
}
