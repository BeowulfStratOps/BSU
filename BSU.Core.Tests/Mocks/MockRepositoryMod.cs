using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests.Mocks
{
    public interface IMockedFiles
    {
        void SetFile(string key, string data);
        IReadOnlyDictionary<string, string> GetFiles();
    }

    public class MockRepositoryMod : IRepositoryMod, IMockedFiles
    {
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public string Identifier, DisplayName;
        public bool ThrowErrorLoad;

        public void SetFile(string key, string data)
        {
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return Files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public byte[] GetFile(string path) => Files.GetValueOrDefault(path);
        public string GetDisplayName() => DisplayName;

        public void Load()
        {
            if (ThrowErrorLoad) throw new TestException();
        }

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(new MemoryStream(GetFile(path)), Utils.GetExtension(path));
        }

        public long GetFileSize(string path) => Files[path].Length;

        public int SleepMs = 0;
        public bool NoOp = false;

        public void DownloadTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            for (int i = 0; i < SleepMs; i++)
            {
                if (token.IsCancellationRequested) return;
                Thread.Sleep(1);
            }

            if (!NoOp) fileStream.Write(Files[path]);
        }

        public void UpdateTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            DownloadTo(path, fileStream, updateCallback, token);
        }

        public Uid GetUid() => new Uid();
    }
}