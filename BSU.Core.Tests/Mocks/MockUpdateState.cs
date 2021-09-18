using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.Core.ViewModel;

namespace BSU.Core.Tests.Mocks
{
    internal class MockUpdateState : IModUpdate
    {
        private readonly bool _errorPrepare;
        private readonly bool _errorUpdate;

        public bool CommitCalled, AbortCalled;

        public MockUpdateState(bool errorPrepare, bool errorUpdate)
        {
            _errorPrepare = errorPrepare;
            _errorUpdate = errorUpdate;
            State = UpdateState.Created;
        }

        public Task Prepare(CancellationToken cancellationToken)
        {
            if (State != UpdateState.Created) throw new InvalidOperationException();
            if (_errorPrepare)
                return Error();
            else
                State = UpdateState.Prepared;
            return Task.CompletedTask;
        }

        public Task Update(CancellationToken cancellationToken)
        {
            if (State != UpdateState.Prepared) throw new InvalidOperationException();
            CommitCalled = true;
            if (_errorUpdate)
                return Error();
            else
            {
                State = UpdateState.Updated;
                OnEnded?.Invoke();
            }

            return Task.CompletedTask;
        }

        public bool IsPrepared => State == UpdateState.Prepared;

        private Task Error()
        {
            OnEnded?.Invoke();
            return Task.FromException(new TestException());
        }

        public void Abort()
        {
            if (State != UpdateState.Prepared && State != UpdateState.Created && State != UpdateState.Creating &&
                State != UpdateState.Preparing && State != UpdateState.Updating) throw new InvalidOperationException();
            AbortCalled = true;
            State = UpdateState.Aborted;
            OnEnded?.Invoke();
        }

        private UpdateState State { get; set; }

        public event Action OnEnded;

        public int GetStats()
        {
            throw new NotImplementedException();
        }

        public override string ToString() => State.ToString();
    }

    public enum UpdateState
    {
        NotCreated,
        Creating,
        Created,
        Preparing,
        Prepared,
        Updating,
        Updated,
        Aborted
    }
}
