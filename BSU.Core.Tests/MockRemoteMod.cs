using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.CoreInterface;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    public interface IMockedFiles
    {
        void SetFile(string key, string data);
    }

    public class MockRemoteMod : IRemoteMod, IMockedFiles
    {
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public string Identifier, DisplayName;

        public void SetFile(string key, string data)
        {
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public byte[] GetFile(string path) => Files.GetValueOrDefault(path);
        public string GetDisplayName() => DisplayName;

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public string GetVersionIdentifier()
        {
            throw new System.NotImplementedException();
        }

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(new MemoryStream(GetFile(path)), Utils.GetExtension(path));
        }

        public long GetFileSize(string path) => Files[path].Length;

        public bool BlockUpdate;

        public void DownloadTo(string path, string filePath, Action<long> updateCallback)
        {
            while (BlockUpdate)
            {
                Thread.Sleep(10);
            }
            File.WriteAllBytes(filePath, Files[path]);
        }

        public void UpdateTo(string path, string filePath, Action<long> updateCallback)
        {
            DownloadTo(path, filePath, updateCallback);
        }
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