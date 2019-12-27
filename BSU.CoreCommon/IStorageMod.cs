using System.Collections.Generic;
using System.IO;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface IStorageMod
    {
        string GetDisplayName();
        string GetIdentifier();
        List<string> GetFileList();
        Stream GetFile(string path);
        FileHash GetFileHash(string path);
        IStorage GetStorage();
        void DeleteFile(string path);
        string GetFilePath(string path);
        Uid GetUid();
    }
}