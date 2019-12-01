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
    }

    public interface ISyncState
    {
        long GetBytesToDownload();
        long GetBytesToUpdate();
        int GetRemainingNewFilesCount();
        int GetRemainingDeletedFilesCount();
        int GetRemainingChangedFilesCount();
        void Start();
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
        string GetName();
    }

    public interface ILocalMod
    {
        string GetDisplayName();
        string GetIdentifier();
        List<string> GetFileList();
        Stream GetFile(string path);
        bool FileExists(string path);
        FileHash GetFileHash(string path);
    }
}
