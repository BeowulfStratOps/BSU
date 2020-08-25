using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model
{
    public class RepositoryUpdate
    {
        // TODO: make sure events go through the workerQueue
        // TODO: use callbacks instead of events.

        private readonly List<DownloadInfo> _downloads = new List<DownloadInfo>();
        private bool _doneAdding;
        private List<IUpdateState> _updates = new List<IUpdateState>();
        private Dictionary<IUpdateState, Exception> _exceptions = new Dictionary<IUpdateState, Exception>();
        private bool _allPrepared;

        internal void Add(DownloadInfo download)
        {
            if (_doneAdding) throw new InvalidOperationException();
            download.Promise.OnValue += CheckAllSetUp;
            download.Promise.OnError += CheckAllSetUp;
            _downloads.Add(download);
        }

        internal void Add(IUpdateState updateState)
        {
            _updates.Add(updateState);
        }

        internal void DoneAdding()
        {
            if (_doneAdding) throw new InvalidOperationException();
            _doneAdding = true;
            CheckAllSetUp();
        }

        private void CheckAllSetUp()
        {
            if (!_downloads.All(p => p.Promise.HasError || p.Promise.HasValue)) return;
            var failed = _downloads.Where(p => p.Promise.HasError).ToList();
            var succeeded = _downloads.Where(p => !p.Promise.HasError).ToList();

            foreach (var download in _downloads.Where(d => d.Promise.HasValue))
            {
                _updates.Add(download.Promise.Value);
            }

            void Proceed(bool doContinue)
            {
                if (doContinue)
                    Prepare();
                else
                    Abort();
            }

            OnSetup?.Invoke(succeeded, failed, Proceed);
        }

        private void Abort()
        {
            foreach (var update in _updates)
            {
                update.Abort();
            }
        }

        private void Prepare()
        {
            if (_updates == null) throw new InvalidOperationException();
            foreach (var update in _updates)
            {
                update.OnPrepared += CheckAllPrepared;
                update.OnFinished += e =>
                {
                    if (e != null) _exceptions[update] = e;
                    CheckAllPrepared();
                };
                update.Prepare();
            }
        }

        private void CheckAllPrepared()
        {
            if (_allPrepared) return;
            if (!_updates.All(u => u.IsPrepared || u.IsFinished)) return;
            _allPrepared = true;
            var errored = _updates.Where(u => u.IsFinished)
                .Select(u => new Tuple<IUpdateState, Exception>(u, _exceptions[u])).ToList();
            var succeeded = _updates.Where(u => u.IsPrepared).ToList();

            void Proceed(bool doContinue)
            {
                if (doContinue)
                    Commit();
                else
                    Abort();
            }

            OnPrepared?.Invoke(succeeded, errored, Proceed);
        }

        private void Commit()
        {
            // rollback failed ones
            foreach (var update in _updates.Where(u => !u.IsPrepared))
            {
                update.Abort();
            }
            _updates = _updates.Where(u => u.IsPrepared).ToList();
            foreach (var update in _updates)
            {
                update.OnFinished += _ => CheckAllFinished();
                update.Commit();
            }
        }

        private void CheckAllFinished()
        {
            if (!_updates.All(u => u.IsFinished)) return;
            var errored = _updates.Where(u => _exceptions.ContainsKey(u))
                .Select(u => new Tuple<IUpdateState, Exception>(u, _exceptions[u])).ToList();
            var succeeded = _updates.Where(u => !_exceptions.ContainsKey(u)).ToList();
            OnFinished?.Invoke(succeeded, errored);
        }

        public delegate void SetUpDelegate(List<DownloadInfo> succeeded, List<DownloadInfo> failed, Action<bool> proceed);
        public delegate void PreparedDelegate(List<IUpdateState> succeeded, List<Tuple<IUpdateState, Exception>> failed, Action<bool> proceed);
        public delegate void FinishedDelegate(List<IUpdateState> succeeded, List<Tuple<IUpdateState, Exception>> failed);

        public event SetUpDelegate OnSetup;
        public event PreparedDelegate OnPrepared;
        public event FinishedDelegate OnFinished;
    }

    public class DownloadInfo
    {
        internal DownloadInfo(IModelRepositoryMod repositoryMod, string identifier, Promise<IUpdateState> promise)
        {
            RepositoryMod = repositoryMod;
            Identifier = identifier;
            Promise = promise;
        }

        internal IModelRepositoryMod RepositoryMod { get; }
        internal string Identifier { get; }
        internal Promise<IUpdateState> Promise { get; }
    }
}
