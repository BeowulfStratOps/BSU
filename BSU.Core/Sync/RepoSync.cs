using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    class RepoSync
    {
        private readonly List<WorkUnit> _allActions, _actionsTodo;
        private Exception _error;

        public RepoSync(IRemoteMod remote, ILocalMod local)
        {
            _allActions = new List<WorkUnit>();
            var remoteList = remote.GetFileList();
            var localList = local.GetFileList();
            var localListCopy = new List<string>(localList);
            foreach (var remoteFile in remoteList)
            {
                if (localList.Contains(remoteFile))
                {
                    if (!remote.GetFileHash(remoteFile).Equals(local.GetFileHash(remoteFile)))
                    {
                        _allActions.Add(new UpdateAction(remote, local, remoteFile, remote.GetFileSize(remoteFile)));
                    }

                    localListCopy.Remove(remoteFile);
                }
                else
                {
                    _allActions.Add(new DownloadAction(remote, local, remoteFile, remote.GetFileSize(remoteFile)));
                }
            }

            foreach (var localFile in localListCopy)
            {
                _allActions.Add(new DeleteAction(local, localFile));
            }
            _actionsTodo = new List<WorkUnit>(_allActions);
        }


        public long GetRemainingBytesToDownload() => _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesRemaining());

        public long GetRemainingBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesRemaining());

        public int GetRemainingChangedFilesCount() => _allActions.OfType<UpdateAction>().Count(a => !a.IsDone());

        public int GetRemainingDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count(a => !a.IsDone());

        public int GetRemainingNewFilesCount() => _allActions.OfType<DownloadAction>().Count(a => !a.IsDone());
        public long GetTotalBytesToDownload() => _allActions.OfType<DownloadAction>().Sum(a => a.GetBytesTotal());

        public long GetTotalBytesToUpdate() => _allActions.OfType<UpdateAction>().Sum(a => a.GetBytesTotal());

        public int GetTotalChangedFilesCount() => _allActions.OfType<UpdateAction>().Count();

        public int GetTotalDeletedFilesCount() => _allActions.OfType<DeleteAction>().Count();

        public int GetTotalNewFilesCount() => _allActions.OfType<DownloadAction>().Count();

        public WorkUnit GetWork()
        {
            var work = _actionsTodo.FirstOrDefault();
            if (work != null) _actionsTodo.Remove(work);
            return work;
        }

        public bool IsDone() => HasError() || _allActions.All(a => a.IsDone() || a.HasError());

        public void SetError(Exception e) => _error = e;

        public bool HasError()
        {
            if (_error != null) return true;
            return _allActions.Any(a => a.HasError());
        }

        public Exception GetError()
        {
            if (_error != null) return _error;
            return _allActions.FirstOrDefault(a => a.HasError())?.GetError();
        }
    }
}
