using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        public void Load()
        {
            var dir = new DirectoryInfo(_path);
            foreach (var modDir in dir.EnumerateDirectories())
            {
                _mods.Add(modDir.Name, new FileRepositoryMod(modDir));
            }
        }

        public Dictionary<string, IRepositoryMod> GetMods() => _mods;
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

        public void Load()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var data = File.ReadAllBytes(file.FullName);
                var path = Path.GetRelativePath(_directory.FullName, file.FullName);
                path = "/" + path.ToLowerInvariant().Replace("\\", "/");
                _files.Add(path, data);
            }
        }

        public List<string> GetFileList() => _files.Keys.ToList();

        public FileHash GetFileHash(string path)
        {
            using var stream = new MemoryStream(_files[path], false);
            var extension = path.Split(".")[^1];
            var hash = new SHA1AndPboHash(stream, extension);
            return hash;
        }

        public byte[] GetFile(string path) => _files[path];

        public string GetDisplayName() => $"Test - {_directory.Name}"; // TODO

        public long GetFileSize(string path) => _files[path].LongLength;

        public void DownloadTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            const int bufferSize = 4 * 1024;
            var data = _files[path];
            for (var i = 0; i < data.LongLength; i += bufferSize)
            {
                var size = Math.Min(bufferSize, data.Length - i);
                fileStream.Write(data, i, size);
                Thread.Sleep(1);
            }
            fileStream.SetLength(data.LongLength);
        }

        public void UpdateTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            DownloadTo(path, fileStream, updateCallback, token);
        }

        public int GetUid() => _logger.GetId();
    }
}
