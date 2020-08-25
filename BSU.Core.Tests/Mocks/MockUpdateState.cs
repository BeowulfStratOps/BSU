using System;
using BSU.Core.Model;

namespace BSU.Core.Tests.Mocks
{
    internal class MockUpdateState : IUpdateState
    {
        private readonly bool _errorPrepare;
        private readonly bool _errorUpdate;
        private UpdateStateEnum _state = UpdateStateEnum.Inactive;

        public bool PrepareCalled, CommitCalled, AbortCalled;

        public MockUpdateState(bool errorPrepare, bool errorUpdate)
        {
            _errorPrepare = errorPrepare;
            _errorUpdate = errorUpdate;
        }

        public void Prepare()
        {
            if (_state != UpdateStateEnum.Inactive) throw new InvalidOperationException();
            PrepareCalled = true;
            if (_errorPrepare)
            {
                _state = UpdateStateEnum.Done;
                IsFinished = true;
                OnFinished?.Invoke(new TestException());
            }
            else
            {
                IsPrepared = true;
                _state = UpdateStateEnum.Prepared;
                OnPrepared?.Invoke();
            }
        }

        public void Commit()
        {
            if (_state != UpdateStateEnum.Prepared) throw new InvalidOperationException();
            CommitCalled = true;
            if (_errorUpdate)
            {
                IsFinished = true;
                OnFinished?.Invoke(new TestException());
            }
            else
            {
                _state = UpdateStateEnum.Done;
                IsFinished = true;
                OnFinished?.Invoke(null);
            }
        }

        public void Abort()
        {
            // TODO: done is used for finished and failed. that's bad.
            if (_state != UpdateStateEnum.Prepared && _state != UpdateStateEnum.Done) throw new InvalidOperationException();
            AbortCalled = true;
            _state = UpdateStateEnum.Done;
            OnFinished?.Invoke(null);
        }

        public event Action OnPrepared;
        public event Action<Exception> OnFinished;
        public int GetPrepStats()
        {
            throw new NotImplementedException();
        }

        public bool IsPrepared { get; private set; }
        public bool IsFinished { get; private set; }
    }
}
