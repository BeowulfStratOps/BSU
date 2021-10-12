using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;
using NLog;

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
        private readonly Action<MockRepositoryMod> _load;

        public void SetFile(string key, string data)
        {
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return Files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public Task<byte[]> GetFile(string path, CancellationToken cancellationToken) => Task.FromResult(Files.GetValueOrDefault(path));

        public void Load()
        {
            if (ThrowErrorLoad) throw new TestException();
            _load?.Invoke(this);
        }

        public Task<List<string>> GetFileList(CancellationToken cancellationToken) => Task.FromResult(Files.Keys.ToList());

        public string GetIdentifier() => Identifier;

        /*public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(new MemoryStream(GetFile(path)), Utils.GetExtension(path));
        }*/

        public long GetFileSize(string path) => Files[path].Length;

        public int SleepMs = 0;
        public bool NoOp = false;

        public MockRepositoryMod(Action<MockRepositoryMod> load = null)
        {
            _load = load;
        }

        public Task DownloadTo(string path, Stream fileStream, IProgress<ulong> progress, CancellationToken token)
        {
            for (int i = 0; i < SleepMs; i++)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }

            if (!NoOp) fileStream.Write(Files[path]);
            return Task.CompletedTask;
        }

        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            var data = await GetFile(path, cancellationToken);
            using var stream = new MemoryStream(data);
            var hash = await SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None);
            return hash;
        }

        public Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken)
        {
            return Task.FromResult((DisplayName, "?"));
        }

        public async Task<ulong> GetFileSize(string path, CancellationToken cancellationToken)
        {
            return (ulong)(await GetFile(path, cancellationToken)).LongLength;
        }

        public Task UpdateTo(string path, Stream fileStream, IProgress<ulong> progress, CancellationToken token)
        {
            return DownloadTo(path, fileStream, progress, token);
        }
    }
}
