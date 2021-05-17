using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Updating;
using BSU.Core.Model.Utility;

namespace BSU.Core.Tests.Mocks
{
    internal class MockUpdateState : IUpdateCreate, IUpdateCreated, IUpdatePrepared, IUpdateDone
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

        public Task<IUpdateCreated> Create()
        {
            if (State == UpdateState.Created) return Task.FromResult<IUpdateCreated>(this);
            if (State != UpdateState.NotCreated) throw new InvalidOperationException();
            if (_errorCreate)
                return Error<IUpdateCreated>();
            else
                State = UpdateState.Created;
            return Task.FromResult<IUpdateCreated>(this);
        }

        public Task<IUpdatePrepared> Prepare()
        {
            if (State != UpdateState.Created) throw new InvalidOperationException();
            if (_errorPrepare)
                return Error<IUpdatePrepared>();
            else
                State = UpdateState.Prepared;
            return Task.FromResult<IUpdatePrepared>(this);
        }

        public Task<IUpdateDone> Update()
        {
            if (State != UpdateState.Prepared) throw new InvalidOperationException();
            CommitCalled = true;
            if (_errorUpdate)
                return Error<IUpdateDone>();
            else
            {
                State = UpdateState.Updated;
                OnEnded?.Invoke();
            }

            return Task.FromResult<IUpdateDone>(this);
        }

        private Task<T> Error<T>()
        {
            OnEnded?.Invoke();
            return Task.FromException<T>(new TestException());
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

        private UpdateState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                OnStateChange?.Invoke();
            }
        }

        public event Action OnStateChange;
        public event Action OnEnded;

        public MatchHash GetTargetMatch()
        {
            throw new NotImplementedException();
        }

        public VersionHash GetTargetVersion()
        {
            throw new NotImplementedException();
        }

        public int GetStats()
        {
            throw new NotImplementedException();
        }

        public IProgressProvider ProgressProvider { get; } = new ProgressProvider();

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
