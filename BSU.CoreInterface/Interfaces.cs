using System;
using System.Collections.Generic;
using System.IO;

namespace BSU.CoreInterface
{
    public interface IRepository
    {
        List<IRemoteMod> GetMods();
        string GetName();
    }

    public interface IRemoteMod
    {
        List<ILocalMod> GetMatchingMods(List<ILocalMod> allLocalMods);
        bool IsVersionMatching(ILocalMod localMod);
        string GetVersionDisplayName();
        ISyncState PrepareSync(ILocalMod target);
    }

    public interface ISyncState
    {
        long GetBytesToDownload();
        long GetBytesToUpdate();
        int GetNewFiles();
        int GetDeletedFiles();
        int GetChangedFiles();
    }

    public interface IStorage
    {
        List<ILocalMod> GetMods();
    }

    public interface ILocalMod
    {
        bool CanWrite();
        DirectoryInfo GetBaseDirectory();
        string GetVersionDisplayName();
    }
}
