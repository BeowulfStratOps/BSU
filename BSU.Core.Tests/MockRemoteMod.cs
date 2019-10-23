using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core.Tests
{
    public class MockRemoteMod : IRemoteMod
    {
        public Dictionary<string, string> Files = new Dictionary<string, string>();
        public string Identifier, DisplayName;

        public byte[] GetFile(string path) => Encoding.UTF8.GetBytes(Files[path]);
        public string GetDisplayName() => DisplayName;

        public List<IModFileInfo> GetFileList() =>
            Files.Select(kv => (IModFileInfo) new MockModFileInfo(kv.Key, kv.Value)).ToList();

        public string GetIdentifier() => Identifier;

        public string GetVersionIdentifier()
        {
            throw new System.NotImplementedException();
        }

        public ISyncState PrepareSync(ILocalMod target)
        {
            throw new System.NotImplementedException();
        }
    }

    class MockModFileInfo : IModFileInfo
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