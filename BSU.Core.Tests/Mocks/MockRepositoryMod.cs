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
        private readonly Logger _logger = EntityLogger.GetLogger();

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

        public Task DownloadTo(string path, Stream fileStream, IProgress<long> updateCallback, CancellationToken token)
        {
            for (int i = 0; i < SleepMs; i++)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }

            if (!NoOp) fileStream.Write(Files[path]);
            return Task.CompletedTask;
        }

        public int GetUid() => _logger.GetId();

        public Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            var data = GetFile(path, cancellationToken).Result;
            using var stream = new MemoryStream(data);
            var hash = SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None).Result;
            return Task.FromResult<FileHash>(hash);
        }

        public Task<string> GetDisplayName(CancellationToken cancellationToken)
        {
            return Task.FromResult(DisplayName);
        }

        public Task<long> GetFileSize(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetFile(path, cancellationToken).Result.LongLength);
        }

        public Task UpdateTo(string path, Stream fileStream, IProgress<long> updateCallback, CancellationToken token)
        {
            return DownloadTo(path, fileStream, updateCallback, token);
        }
    }
}
