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
        private readonly Task _load;
        private readonly Dictionary<string, byte[]> _files = new();

        public MockRepositoryMod(Task? load = null)
        {
            _load = load ?? Task.CompletedTask;
        }

        public void SetFile(string key, string data)
        {
            _files[key] = Encoding.UTF8.GetBytes(data);
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return _files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public async Task<byte[]> GetFile(string path, CancellationToken cancellationToken)
        {
            await _load;
            return _files[path];
        }


        public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
        {
            await _load;
            return _files.Keys.ToList();
        }

        public async Task DownloadTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);
            await using var fileStream = await fileSystem.OpenWrite(path, cancellationToken);
            await fileStream.WriteAsync(_files[path], cancellationToken);
        }

        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            var data = await GetFile(path, cancellationToken);
            await using var stream = new MemoryStream(data);
            var hash = await SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None);
            return hash;
        }

        public async Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken)
        {
            return ("???", "?");
        }

        public async Task<ulong> GetFileSize(string path, CancellationToken cancellationToken)
        {
            return (ulong)(await GetFile(path, cancellationToken)).LongLength;
        }

        public async Task UpdateTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            await DownloadTo(path, fileSystem, progress, cancellationToken);
        }
    }
}
