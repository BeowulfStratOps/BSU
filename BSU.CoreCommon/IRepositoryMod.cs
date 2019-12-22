using System;
using System.Collections.Generic;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface IRepositoryMod
    {
        List<string> GetFileList();
        FileHash GetFileHash(string path);
        byte[] GetFile(string path);
        string GetDisplayName();
        string GetIdentifier();
        long GetFileSize(string path);
        void DownloadTo(string path, string filePath, Action<long> updateCallback);
        void UpdateTo(string path, string filePath, Action<long> updateCallback);
        Uid GetUid();
    }
}
