using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core.Tests
{
    public class MockStorageMod : ILocalMod
    {
        public string Identifier, DisplayName;

        public Dictionary<string, string> Files = new Dictionary<string, string>();

        public bool FileExists(string path) => Files.ContainsKey(path);

        public string GetDisplayName() => DisplayName;

        public Stream GetFile(string path) => new MemoryStream(Encoding.UTF8.GetBytes(Files[path]));

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;
    }
}