using System;
using System.Collections.Generic;
using System.IO;

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

    public interface IModFileInfo
    {
        string GetPath();
        byte[] GetFileHash();
    }

    public interface IRemoteMod
    {
        List<IModFileInfo> GetFileList();
        byte[] DownloadFile(string path);
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
        int GetNewFilesCount();
        int GetDeletedFilesCount();
        int GetChangedFilesCount();
        void Start();
        bool IsDone();
        event EventHandler Update;
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
        DirectoryInfo GetBaseDirectory();
        string GetDisplayName();
        string GetIdentifier();
    }
}
