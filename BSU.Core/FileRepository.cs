using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;
using NLog;

namespace BSU.Core
{
    public class FileRepository : IRepository
    {
        private readonly string _path;
        private readonly Dictionary<string, IRepositoryMod> _mods = new();

        public FileRepository(string path)
        {
            _path = path;
        }

        public async Task Load(CancellationToken cancellationToken)
        {
            var dir = new DirectoryInfo(_path);
            foreach (var modDir in dir.EnumerateDirectories())
            {
                _mods.Add(modDir.Name, new FileRepositoryMod(modDir));
            }
        }

        public Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken) => Task.FromResult(_mods);
    }

    internal class FileRepositoryMod : IRepositoryMod
    {
        private readonly DirectoryInfo _directory;
        private readonly Logger _logger = EntityLogger.GetLogger();

        // TODO: terrible performance
        private readonly Dictionary<string, byte[]> _files = new();

        public FileRepositoryMod(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public async Task Load(CancellationToken cancellationToken)
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var data = await File.ReadAllBytesAsync(file.FullName, cancellationToken);
                var path = Path.GetRelativePath(_directory.FullName, file.FullName);
                path = "/" + path.ToLowerInvariant().Replace("\\", "/");
                _files.Add(path, data);
            }
        }

        public Task<List<string>> GetFileList(CancellationToken cancellationToken) => Task.FromResult<List<string>>(_files.Keys.ToList());

        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream(_files[path], false);
            var extension = path.Split(".")[^1];
            var hash = await SHA1AndPboHash.BuildAsync(stream, extension, cancellationToken);
            return hash;
        }

        public Task<byte[]> GetFile(string path, CancellationToken cancellationToken) => Task.FromResult<byte[]>(_files[path]);

        public Task<string> GetDisplayName(CancellationToken cancellationToken) => Task.FromResult($"Test - {_directory.Name}"); // TODO

        public Task<long> GetFileSize(string path, CancellationToken cancellationToken) => Task.FromResult(_files[path].LongLength);

        public async Task DownloadTo(string path, Stream fileStream, IProgress<long> progress, CancellationToken token)
        {
            const int bufferSize = 4 * 1024;
            var data = _files[path];
            for (var i = 0; i < data.LongLength; i += bufferSize)
            {
                var size = Math.Min(bufferSize, data.Length - i);
                await fileStream.WriteAsync(data, i, size, token);
                Thread.Sleep(1);
            }
            fileStream.SetLength(data.LongLength);
        }

        public async Task UpdateTo(string path, Stream fileStream, IProgress<long> progress, CancellationToken token)
        {
            await DownloadTo(path, fileStream, progress, token);
        }

        public int GetUid() => _logger.GetId();
    }
}
