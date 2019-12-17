using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    internal class MockStorageMod : ILocalMod, IMockedFiles
    {
        public string Identifier, DisplayName;
        public MockStorage Storage;

        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

        public void SetFile(string key, string data)
        {
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public string GetFileContent(string key) => Encoding.UTF8.GetString(Files[key]);

        public void DeleteFile(string path) => Files.Remove(path);

        public bool FileExists(string path) => Files.ContainsKey(path);

        public string GetSpecialFilePath(string path) => path;

        public string GetDisplayName() => DisplayName;

        public Stream GetFile(string path)
        {
            return Files.ContainsKey(path) ? new MemoryStream(Files[path]) : null;
        }

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(GetFile(path), Utils.GetExtension(path));
        }

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public IStorage GetStorage() => Storage;

        public string GetFilePath(string path)
        {
            throw new NotSupportedException();
        }
    }
}