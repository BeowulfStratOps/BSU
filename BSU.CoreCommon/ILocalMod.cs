using System.Collections.Generic;
using System.IO;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface ILocalMod
    {
        string GetDisplayName();
        string GetIdentifier();
        List<string> GetFileList();
        Stream GetFile(string path);
        FileHash GetFileHash(string path);
        IStorage GetStorage();
        void DeleteFile(string path);
        string GetFilePath(string path);
    }
}
