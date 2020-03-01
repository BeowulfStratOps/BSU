using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    internal class MockStorageMod : IStorageMod, IMockedFiles
    {
        public string Identifier;
        public MockStorage Storage;
        public bool Locked = false;

        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

        public void SetFile(string key, string data)
        {
            if (Locked) throw new IOException("File in use");
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public string GetFileContent(string key)
        {
            if (Locked) throw new IOException("File in use");
            return Encoding.UTF8.GetString(Files[key]);
        }

        public void DeleteFile(string path)
        {
            if (Locked) throw new IOException("File in use");
            Files.Remove(path);
        }

        public string GetDisplayName() => throw new NotImplementedException();

        public Stream GetFile(string path)
        {
            if (Locked) throw new IOException("File in use");
            return Files.ContainsKey(path) ? new MemoryStream(Files[path]) : null;
        }

        public FileHash GetFileHash(string path)
        {
            if (Locked) throw new IOException("File in use");
            return new SHA1AndPboHash(GetFile(path), Utils.GetExtension(path));
        }

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public IStorage GetStorage() => Storage;

        public string GetFilePath(string path)
        {
            if (Locked) throw new IOException("File in use");
            return "";
        }

        public Uid GetUid() => new Uid();

        public void Load()
        {
            
        }
    }
}
