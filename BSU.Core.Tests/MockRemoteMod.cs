using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BSU.CoreInterface;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    public class MockRemoteMod : IRemoteMod
    {
        public Dictionary<string, string> Files = new Dictionary<string, string>();
        public string Identifier, DisplayName;

        public byte[] GetFile(string path) => Encoding.UTF8.GetBytes(Files[path]);
        public string GetDisplayName() => DisplayName;

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public string GetVersionIdentifier()
        {
            throw new System.NotImplementedException();
        }

        public ISyncState PrepareSync(ILocalMod target)
        {
            throw new System.NotImplementedException();
        }

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(new MemoryStream(GetFile(path)), Utils.GetExtension(path));
        }

        public long GetFileSize(string path) => Files[path].Length;
    }

    class MockModFileInfo
    {
        private readonly string _path;
        private readonly string _content;

        public MockModFileInfo(string path, string content)
        {
            _path = path;
            _content = content;
        }

        public byte[] GetFileHash()
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(_content));
        }

        public string GetPath() => _path;
    }
}