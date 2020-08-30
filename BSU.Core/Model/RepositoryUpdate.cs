﻿using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model
{
    public class RepositoryUpdate
    {
        // TODO: make sure events go through the workerQueue

        private readonly SetUpDelegate _setUpCallback;
        private readonly PreparedDelegate _preparedCallback;
        private readonly FinishedDelegate _finishedCallback;

        private readonly List<DownloadInfo> _downloads = new List<DownloadInfo>();
        private bool _doneAdding;
        private List<IUpdateState> _updates = new List<IUpdateState>();
        private Dictionary<IUpdateState, Exception> _exceptions = new Dictionary<IUpdateState, Exception>();
        private bool _allPrepared;

        public RepositoryUpdate(SetUpDelegate setUpCallback, PreparedDelegate preparedCallback, FinishedDelegate finishedCallback)
        {
            _setUpCallback = setUpCallback;
            _preparedCallback = preparedCallback;
            _finishedCallback = finishedCallback;
        }

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

            _setUpCallback(new SetUpArgs(succeeded, failed), Proceed);
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

            _preparedCallback(new PreparedArgs(succeeded, errored), Proceed);
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
            _finishedCallback(new FinishedArgs(succeeded, errored));
        }

        public delegate void SetUpDelegate(SetUpArgs args, Action<bool> proceed);
        public delegate void PreparedDelegate(PreparedArgs args, Action<bool> proceed);
        public delegate void FinishedDelegate(FinishedArgs args);
    }

    public class FinishedArgs
    {
        public List<IUpdateState> Succeeded { get; }
        public List<Tuple<IUpdateState, Exception>> Failed { get; }

        public FinishedArgs(List<IUpdateState> succeeded, List<Tuple<IUpdateState, Exception>> failed)
        {
            Succeeded = succeeded;
            Failed = failed;
        }
    }

    public class PreparedArgs
    {
        public List<IUpdateState> Succeeded { get; }
        public List<Tuple<IUpdateState, Exception>> Failed { get; }

        public PreparedArgs(List<IUpdateState> succeeded, List<Tuple<IUpdateState, Exception>> failed)
        {
            Succeeded = succeeded;
            Failed = failed;
        }
    }

    public class SetUpArgs
    {
        public List<DownloadInfo> Succeeded { get; }
        public List<DownloadInfo> Failed { get; }

        public SetUpArgs(List<DownloadInfo> succeeded, List<DownloadInfo> failed)
        {
            Succeeded = succeeded;
            Failed = failed;
        }
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
