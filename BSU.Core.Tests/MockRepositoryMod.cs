using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    public interface IMockedFiles
    {
        void SetFile(string key, string data);
    }

    public class MockRepositoryMod : IRepositoryMod, IMockedFiles
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

        public Uid GetUid() => new Uid();
    }
}
