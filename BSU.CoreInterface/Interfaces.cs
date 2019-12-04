using System;
using System.Collections.Generic;
using System.IO;
using BSU.Hashes;

namespace BSU.CoreInterface
{
    public interface IRepository
    {
        List<IRemoteMod> GetMods();

        /// <summary>
        /// alias. identifier of sorts.
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Url or smth
        /// </summary>
        /// <returns></returns>
        string GetLocation();
    }

    public interface IRemoteMod
    {
        List<string> GetFileList();
        FileHash GetFileHash(string path);
        byte[] GetFile(string path);
        string GetDisplayName();

        /// <summary>
        /// diff and ready to sync
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        ISyncState PrepareSync(ILocalMod target);
        string GetIdentifier();
        string GetVersionIdentifier();
        long GetFileSize(string path);
    }

    public interface ISyncState
    {
        long GetRemainingBytesToDownload();
        long GetRemainingBytesToUpdate();
        int GetRemainingNewFilesCount();
        int GetRemainingDeletedFilesCount();
        int GetRemainingChangedFilesCount();
        long GetTotalBytesToDownload();
        long GetTotalBytesToUpdate();
        int GetTotalNewFilesCount();
        int GetTotalDeletedFilesCount();
        int GetTotalChangedFilesCount();
        IWorkUnit GetWork();
        bool IsDone();
    }

    public interface IWorkUnit
    {
        void DoWork();
        bool IsDone();
    }

    public interface IStorage
    {
        /// <summary>
        /// false for e.g. steam
        /// </summary>
        /// <returns></returns>
        bool CanWrite();

        List<ILocalMod> GetMods();

        /// <summary>
        /// path or smth
        /// </summary>
        /// <returns></returns>
        string GetLocation();

        /// <summary>
        /// alias. identifier of sorts
        /// </summary>
        /// <returns></returns>
        string GetIdentifier();
    }

    public interface ILocalMod
    {
        string GetDisplayName();
        string GetIdentifier();
        List<string> GetFileList();
        Stream GetFile(string path);
        bool FileExists(string path);
        FileHash GetFileHash(string path);
        IStorage GetStorage();
    }
}
