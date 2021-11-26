using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly Dictionary<string, byte[]> _files = new();

        public void SetFile(string key, string data)
        {
            _files[key] = Encoding.UTF8.GetBytes(data);
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return _files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public Task<byte[]> GetFile(string path, CancellationToken cancellationToken) => Task.FromResult(_files.GetValueOrDefault(path));


        public Task<List<string>> GetFileList(CancellationToken cancellationToken) => Task.FromResult(_files.Keys.ToList());

        public async Task DownloadTo(string path, Stream fileStream, IProgress<ulong> progress, CancellationToken token)
        {
            await Task.Delay(5, token);
            fileStream.Write(_files[path]);
        }

        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            var data = await GetFile(path, cancellationToken);
            using var stream = new MemoryStream(data);
            var hash = await SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None);
            return hash;
        }

        public async Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken)
        {
            return (null, "?");
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
