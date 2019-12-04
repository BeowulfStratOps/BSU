using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.CoreInterface;

namespace BSU.BSO
{
    class RepoSync : ISyncState
    {
        private readonly List<IWorkUnit> _allActions, _actionsTodo;

        public RepoSync(BsoRepoMod remote, ILocalMod local)
        {
            _allActions = new List<IWorkUnit>();
            var remoteList = remote.GetFileList();
            var localList = local.GetFileList();
            var localListCopy = new List<string>(localList);
            foreach (var remoteFile in remoteList)
            {
                if (localList.Contains(remoteFile))
                {
                    if (!remote.GetFileHash(remoteFile).Equals(local.GetFileHash(remoteFile)))
                    {
                        _allActions.Add(new UpdateAction(remoteFile, remote.GetFileSize(remoteFile)));
                    }

                    localListCopy.Remove(remoteFile);
                }
                else
                {
                    _allActions.Add(new DownloadAction(remoteFile, remote.GetFileSize(remoteFile)));
                }
            }

            foreach (var localFile in localListCopy)
            {
                _allActions.Add(new DeleteAction(localFile));
            }
            _actionsTodo = new List<IWorkUnit>(_allActions);
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

        public IWorkUnit GetWork()
        {
            var work = _actionsTodo.FirstOrDefault();
            if (work != null) _actionsTodo.Remove(work);
            return work;
        }

        public bool IsDone() => _allActions.All(a => a.IsDone());
    }
}
